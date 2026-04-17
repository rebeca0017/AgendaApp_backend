using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using AgendamientoCitas.Dtos;
using AgendamientoCitas.Filtros;
using AgendamientoCitas.Repositorios;
using AgendamientoCitas.Servicios;
using AgendamientoCitas.Utilidades;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AgendamientoCitas.Endpoints
{
    static class UsuariosEndpoints
    {
        private const string NombreClaim = "nombre";
        private const string ApellidoClaim = "apellido";
        private const string AdminClaim = "admin";
        private const string DebeCambiarPasswordClaim = "debeCambiarPassword";

        public static RouteGroupBuilder MapUsuarios(this RouteGroupBuilder group)
        {
            group.MapPost("/registrar", Registrar)
                .AddEndpointFilter<FiltroValidaciones<CredencialesUsuarioDTO>>();

            group.MapPost("/login", Login)
               .AddEndpointFilter<FiltroValidaciones<CredencialesUsuarioDTO>>();

            group.MapPost("/solicitarrecuperacionpassword", SolicitarRecuperacionPassword)
               .AddEndpointFilter<FiltroValidaciones<SolicitarRecuperacionPasswordDTO>>();

            group.MapPost("/recuperarpassword", RecuperarPassword)
               .AddEndpointFilter<FiltroValidaciones<RecuperarPasswordDTO>>();

            group.MapGet("/renovartoken", RenovarToken).RequireAuthorization();

            group.MapPut("/modificar", ModificarUsuario)
            .RequireAuthorization();

            group.MapPost("/cambiarpassword", CambiarPassword)
            .RequireAuthorization();

            group.MapGet("/admin/usuarios", ListarUsuariosAdmin)
            .RequireAuthorization();

            group.MapPost("/admin/enviarrecuperacionpassword", EnviarRecuperacionPasswordAdmin)
            .RequireAuthorization()
            .AddEndpointFilter<FiltroValidaciones<AdminAccionUsuarioDTO>>();

            group.MapPost("/admin/generarpasswordtemporal", GenerarPasswordTemporalAdmin)
            .RequireAuthorization()
            .AddEndpointFilter<FiltroValidaciones<AdminAccionUsuarioDTO>>();

            return group;
        }

        static async Task<Results<Ok<RespuestaAutenticacionDTO>, BadRequest<IEnumerable<IdentityError>>>>Registrar(CredencialesUsuarioDTO credencialesUsuarioDTO,
            [FromServices] UserManager<IdentityUser> userManager, IConfiguration configuration)
        {
            var email = credencialesUsuarioDTO.user.Trim();
            var nombre = credencialesUsuarioDTO.Nombre?.Trim() ?? string.Empty;
            var apellido = credencialesUsuarioDTO.Apellido?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(apellido))
            {
                return TypedResults.BadRequest<IEnumerable<IdentityError>>([
                    new IdentityError { Description = "El nombre y apellido son obligatorios." }
                ]);
            }

            var usuario = new IdentityUser
            {
                UserName = email,
                Email = email
            };

            var resultado = await userManager.CreateAsync(usuario, credencialesUsuarioDTO.pass.Trim());

            if (resultado.Succeeded)
            {
                await userManager.AddClaimsAsync(usuario, [
                    new Claim(NombreClaim, nombre),
                    new Claim(ApellidoClaim, apellido)
                ]);

                var respuestaAutenticacion =
                    await ConstruirToken(usuario.Email!, configuration, userManager);
                return TypedResults.Ok(respuestaAutenticacion);
            }
            else
            {
                return TypedResults.BadRequest(resultado.Errors);
            }
        }

        static async Task<Results<Ok<RespuestaAutenticacionDTO>, BadRequest<string>>> Login(CredencialesUsuarioDTO credencialesUsuarioDTO, 
            [FromServices] SignInManager<IdentityUser> signInManager,
            [FromServices] UserManager<IdentityUser> userManager, IConfiguration configuration)
        {
            var usuario = await BuscarUsuarioPorEmailAsync(userManager, credencialesUsuarioDTO.user.Trim());

            if (usuario is null)
            {
                return TypedResults.BadRequest("Login incorrecto");
            }

            var resultado = await signInManager.CheckPasswordSignInAsync(usuario,
                credencialesUsuarioDTO.pass.Trim(), lockoutOnFailure: false);

            if (resultado.Succeeded)
            {
                var respuestaAutenticacion = await ConstruirToken(usuario.Email!, configuration, userManager);
                return TypedResults.Ok(respuestaAutenticacion);
            }
            else
            {
                return TypedResults.BadRequest("Login incorrecto");
            }
        }

        static async Task<IResult> SolicitarRecuperacionPassword(SolicitarRecuperacionPasswordDTO dto,
            [FromServices] UserManager<IdentityUser> userManager,
            [FromServices] IServicioEmail servicioEmail)
        {
            var usuario = await BuscarUsuarioPorEmailAsync(userManager, dto.Email.Trim());

            if (usuario is not null && !string.IsNullOrWhiteSpace(usuario.Email))
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(usuario);
                await servicioEmail.EnviarRecuperacionPasswordAsync(usuario.Email, token);
            }

            return Results.NoContent();
        }

        static async Task<IResult> RecuperarPassword(RecuperarPasswordDTO dto,
            [FromServices] UserManager<IdentityUser> userManager)
        {
            var usuario = await BuscarUsuarioPorEmailAsync(userManager, dto.Email.Trim());

            if (usuario is null)
            {
                return Results.BadRequest("La solicitud de recuperacion no es valida.");
            }

            var resultado = await userManager.ResetPasswordAsync(usuario, dto.Token, dto.PasswordNueva.Trim());

            if (resultado.Succeeded)
            {
                return Results.NoContent();
            }

            return Results.BadRequest(resultado.Errors);
        }

        public async static Task<Results<Ok<RespuestaAutenticacionDTO>, NotFound>>RenovarToken(IServicioUsuarios servicioUsuarios, IConfiguration configuration,
            [FromServices] UserManager<IdentityUser> userManager)
        {
            var usuario = await servicioUsuarios.ObtenerUsuario();

            if (usuario is null)
            {
                return TypedResults.NotFound();
            }

            var respuestaAutenticacionDTO = await ConstruirToken(usuario.Email!, configuration,
                userManager);

            return TypedResults.Ok(respuestaAutenticacionDTO);
        }


        private async static Task<RespuestaAutenticacionDTO>ConstruirToken(string email,IConfiguration configuration, UserManager<IdentityUser> userManager)
        {
            var claims = new List<Claim>
            {
                new Claim("user", email)
            };

            var usuario = await BuscarUsuarioPorEmailAsync(userManager, email);
            if (usuario is null)
            {
                throw new InvalidOperationException("No se pudo construir el token para un usuario inexistente.");
            }

            var claimsDB = await userManager.GetClaimsAsync(usuario);

            claims.AddRange(claimsDB);
            var nombre = ObtenerValorClaim(claimsDB, NombreClaim);
            var apellido = ObtenerValorClaim(claimsDB, ApellidoClaim);
            var esAdmin = EsUsuarioAdmin(email, claimsDB, configuration);
            var debeCambiarPassword = EsClaimVerdadero(claimsDB, DebeCambiarPasswordClaim);

            if (esAdmin)
            {
                claims.Add(new Claim(AdminClaim, "true"));
            }

            var llave = Llaves.ObtenerLlave(configuration);
            var creds = new SigningCredentials(llave.First(), SecurityAlgorithms.HmacSha256);

            var expiracion = DateTime.UtcNow.AddYears(1);

            var tokenDeSeguridad = new JwtSecurityToken(issuer: null, audience: null, claims: claims,
                expires: expiracion, signingCredentials: creds);

            var token = new JwtSecurityTokenHandler().WriteToken(tokenDeSeguridad);

            return new RespuestaAutenticacionDTO
            {
                Token = token,
                Expiracion = expiracion,
                Email = email,
                Nombre = nombre,
                Apellido = apellido,
                EsAdmin = esAdmin,
                DebeCambiarPassword = debeCambiarPassword
            };
        }

        static async Task<IResult> ModificarUsuario(ModificarUsuarioDTO dto,IServicioUsuarios servicioUsuarios,
            [FromServices] UserManager<IdentityUser> userManager,IConfiguration configuration)
        {
            var usuario = await servicioUsuarios.ObtenerUsuario();

            if (usuario is null)
            {
                return Results.NotFound();
            }

            var emailActual = dto.EmailActual.Trim();
            var nuevoEmail = dto.NuevoEmail.Trim();
            var nombre = dto.Nombre.Trim();
            var apellido = dto.Apellido.Trim();

            if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(apellido))
            {
                return Results.BadRequest("El nombre y apellido son obligatorios.");
            }

            if (!string.Equals(usuario.Email, emailActual, StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest("El email actual no coincide con la sesión activa.");
            }

            var resultadoEmail = await userManager.SetEmailAsync(usuario, nuevoEmail);

            if (!resultadoEmail.Succeeded)
            {
                return Results.BadRequest(resultadoEmail.Errors);
            }

            var resultadoUserName = await userManager.SetUserNameAsync(usuario, nuevoEmail);

            if (!resultadoUserName.Succeeded)
            {
                return Results.BadRequest(resultadoUserName.Errors);
            }

            await ActualizarClaimAsync(userManager, usuario, NombreClaim, nombre);
            await ActualizarClaimAsync(userManager, usuario, ApellidoClaim, apellido);

            var resultado = await userManager.UpdateAsync(usuario);

            if (resultado.Succeeded)
            {
                return Results.Ok(await ConstruirToken(nuevoEmail, configuration, userManager));
            }

            return Results.BadRequest(resultado.Errors);
        }

        static async Task<IResult> CambiarPassword(CambiarPasswordDTO dto,IServicioUsuarios servicioUsuarios,UserManager<IdentityUser> userManager)
        {
            var usuario = await servicioUsuarios.ObtenerUsuario();

            if (usuario is null)
            {
                return Results.NotFound();
            }

            var resultado = await userManager.ChangePasswordAsync(
                usuario,
                dto.PasswordActual.Trim(),
                dto.PasswordNueva.Trim()
            );

            if (resultado.Succeeded)
            {
                await ActualizarClaimAsync(userManager, usuario, DebeCambiarPasswordClaim, "false");
                return Results.NoContent();
            }

            return Results.BadRequest(resultado.Errors);
        }

        static async Task<IResult> ListarUsuariosAdmin(IServicioUsuarios servicioUsuarios,
            IRepositorioUsuarios repositorioUsuarios,
            [FromServices] UserManager<IdentityUser> userManager,
            IConfiguration configuration)
        {
            if (!await EsAdminActualAsync(servicioUsuarios, userManager, configuration))
            {
                return Results.Forbid();
            }

            var usuarios = await repositorioUsuarios.ListarUsuarios();
            var respuesta = new List<UsuarioAdminDTO>();

            foreach (var usuario in usuarios.Where(usuario => !string.IsNullOrWhiteSpace(usuario.Email)))
            {
                var claims = await userManager.GetClaimsAsync(usuario);
                respuesta.Add(new UsuarioAdminDTO
                {
                    Id = usuario.Id,
                    Email = usuario.Email!,
                    EmailConfirmed = usuario.EmailConfirmed,
                    EsAdmin = EsUsuarioAdmin(usuario.Email!, claims, configuration)
                });
            }

            return Results.Ok(respuesta);
        }

        static async Task<IResult> EnviarRecuperacionPasswordAdmin(AdminAccionUsuarioDTO dto,
            IServicioUsuarios servicioUsuarios,
            [FromServices] UserManager<IdentityUser> userManager,
            [FromServices] IServicioEmail servicioEmail,
            IConfiguration configuration)
        {
            if (!await EsAdminActualAsync(servicioUsuarios, userManager, configuration))
            {
                return Results.Forbid();
            }

            var usuario = await BuscarUsuarioPorEmailAsync(userManager, dto.Email.Trim());

            if (usuario is null || string.IsNullOrWhiteSpace(usuario.Email))
            {
                return Results.NotFound("Usuario no encontrado.");
            }

            var token = await userManager.GeneratePasswordResetTokenAsync(usuario);
            await servicioEmail.EnviarRecuperacionPasswordAsync(usuario.Email, token);

            return Results.NoContent();
        }

        static async Task<IResult> GenerarPasswordTemporalAdmin(AdminAccionUsuarioDTO dto,
            IServicioUsuarios servicioUsuarios,
            [FromServices] UserManager<IdentityUser> userManager,
            [FromServices] IServicioEmail servicioEmail,
            IConfiguration configuration)
        {
            if (!await EsAdminActualAsync(servicioUsuarios, userManager, configuration))
            {
                return Results.Forbid();
            }

            var usuario = await BuscarUsuarioPorEmailAsync(userManager, dto.Email.Trim());

            if (usuario is null || string.IsNullOrWhiteSpace(usuario.Email))
            {
                return Results.NotFound("Usuario no encontrado.");
            }

            var passwordTemporal = GenerarPasswordTemporal();
            var token = await userManager.GeneratePasswordResetTokenAsync(usuario);
            var resultado = await userManager.ResetPasswordAsync(usuario, token, passwordTemporal);

            if (!resultado.Succeeded)
            {
                return Results.BadRequest(resultado.Errors);
            }

            await ActualizarClaimAsync(userManager, usuario, DebeCambiarPasswordClaim, "true");
            await servicioEmail.EnviarPasswordTemporalAsync(usuario.Email, passwordTemporal);

            return Results.NoContent();
        }

        private static Task<IdentityUser?> BuscarUsuarioPorEmailAsync(UserManager<IdentityUser> userManager,string email)
        {
            var normalizedEmail = userManager.NormalizeEmail(email);
            return userManager.FindByEmailAsync(normalizedEmail ?? email);
        }

        private static string ObtenerValorClaim(IEnumerable<Claim> claims, string tipo)
            => claims.FirstOrDefault(claim => claim.Type == tipo)?.Value ?? string.Empty;

        private static bool EsClaimVerdadero(IEnumerable<Claim> claims, string tipo)
            => string.Equals(ObtenerValorClaim(claims, tipo), "true", StringComparison.OrdinalIgnoreCase);

        private static bool EsUsuarioAdmin(string email, IEnumerable<Claim> claims, IConfiguration configuration)
        {
            if (EsClaimVerdadero(claims, AdminClaim))
            {
                return true;
            }

            var adminEmails = configuration.GetSection("Security:AdminEmails").Get<string[]>() ?? [];
            return adminEmails.Any(adminEmail => string.Equals(adminEmail, email, StringComparison.OrdinalIgnoreCase));
        }

        private static async Task<bool> EsAdminActualAsync(IServicioUsuarios servicioUsuarios,
            UserManager<IdentityUser> userManager,
            IConfiguration configuration)
        {
            var usuario = await servicioUsuarios.ObtenerUsuario();

            if (usuario is null || string.IsNullOrWhiteSpace(usuario.Email))
            {
                return false;
            }

            var claims = await userManager.GetClaimsAsync(usuario);
            return EsUsuarioAdmin(usuario.Email, claims, configuration);
        }

        private static string GenerarPasswordTemporal()
        {
            const string mayusculas = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string minusculas = "abcdefghijkmnopqrstuvwxyz";
            const string numeros = "23456789";
            const string simbolos = "!@$?";
            const string todos = mayusculas + minusculas + numeros + simbolos;

            var caracteres = new List<char>
            {
                ObtenerCaracterAleatorio(mayusculas),
                ObtenerCaracterAleatorio(minusculas),
                ObtenerCaracterAleatorio(numeros),
                ObtenerCaracterAleatorio(simbolos)
            };

            while (caracteres.Count < 12)
            {
                caracteres.Add(ObtenerCaracterAleatorio(todos));
            }

            return new string(caracteres.OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue)).ToArray());
        }

        private static char ObtenerCaracterAleatorio(string valores)
            => valores[RandomNumberGenerator.GetInt32(valores.Length)];

        private static async Task ActualizarClaimAsync(UserManager<IdentityUser> userManager,IdentityUser usuario,string tipo,string valor)
        {
            var claims = await userManager.GetClaimsAsync(usuario);
            var claimActual = claims.FirstOrDefault(claim => claim.Type == tipo);

            if (claimActual is null)
            {
                await userManager.AddClaimAsync(usuario, new Claim(tipo, valor));
                return;
            }

            await userManager.ReplaceClaimAsync(usuario, claimActual, new Claim(tipo, valor));
        }
    }
}
