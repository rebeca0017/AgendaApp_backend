using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using AgendamientoCitas.Dtos;
using AgendamientoCitas.Filtros;
using AgendamientoCitas.Servicios;
using AgendamientoCitas.Utilidades;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AgendamientoCitas.Endpoints
{
    static class UsuariosEndpoints
    {
        private const string NombreClaim = "nombre";
        private const string ApellidoClaim = "apellido";

        public static RouteGroupBuilder MapUsuarios(this RouteGroupBuilder group)
        {
            group.MapPost("/registrar", Registrar)
                .AddEndpointFilter<FiltroValidaciones<CredencialesUsuarioDTO>>();

            group.MapPost("/login", Login)
               .AddEndpointFilter<FiltroValidaciones<CredencialesUsuarioDTO>>();

            group.MapPost("/recuperarpassword", RecuperarPassword)
               .AddEndpointFilter<FiltroValidaciones<RecuperarPasswordDTO>>();

            group.MapGet("/renovartoken", RenovarToken).RequireAuthorization();

            group.MapPut("/modificar", ModificarUsuario)
            .RequireAuthorization();

            group.MapPost("/cambiarpassword", CambiarPassword)
            .RequireAuthorization();

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

        static async Task<IResult> RecuperarPassword(RecuperarPasswordDTO dto,
            [FromServices] UserManager<IdentityUser> userManager)
        {
            var usuario = await BuscarUsuarioPorEmailAsync(userManager, dto.Email.Trim());

            if (usuario is null)
            {
                return Results.NotFound(new { mensaje = "Usuario no encontrado" });
            }

            var token = await userManager.GeneratePasswordResetTokenAsync(usuario);
            var resultado = await userManager.ResetPasswordAsync(usuario, token, dto.PasswordNueva.Trim());

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
                Apellido = apellido
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
                return Results.NoContent();
            }

            return Results.BadRequest(resultado.Errors);
        }

        private static Task<IdentityUser?> BuscarUsuarioPorEmailAsync(UserManager<IdentityUser> userManager,string email)
        {
            var normalizedEmail = userManager.NormalizeEmail(email);
            return userManager.FindByEmailAsync(normalizedEmail ?? email);
        }

        private static string ObtenerValorClaim(IEnumerable<Claim> claims, string tipo)
            => claims.FirstOrDefault(claim => claim.Type == tipo)?.Value ?? string.Empty;

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
