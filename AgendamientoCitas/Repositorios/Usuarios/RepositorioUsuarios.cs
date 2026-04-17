using Dapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Claims;

namespace AgendamientoCitas.Repositorios
{
    public class RepositorioUsuarios : IRepositorioUsuarios
    {
        private readonly string connectionString;

        public RepositorioUsuarios(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<IdentityUser?> BuscarUsuarioPorEmail(string normalizedEmail)
        {
            using (var conexion = new SqlConnection(connectionString))
            {
                return await conexion.QuerySingleOrDefaultAsync<IdentityUser>
                    ("Usuarios_BuscarPorEmail", new { normalizedEmail },
                    commandType: CommandType.StoredProcedure);
            }
        }

        public async Task<IdentityUser?> BuscarUsuarioPorId(string id)
        {
            const string sql = "SELECT * FROM Usuarios WHERE Id = @id";

            using var conexion = new SqlConnection(connectionString);
            return await conexion.QuerySingleOrDefaultAsync<IdentityUser>(sql, new { id });
        }

        public async Task<string> Crear(IdentityUser usuario)
        {
            using (var conexion = new SqlConnection(connectionString))
            {
                usuario.Id = Guid.NewGuid().ToString();
                usuario.SecurityStamp ??= Guid.NewGuid().ToString();
                usuario.ConcurrencyStamp ??= Guid.NewGuid().ToString();

                await conexion.ExecuteAsync("Usuarios_Crear", new
                {
                    usuario.Id,
                    usuario.Email,
                    usuario.NormalizedEmail,
                    usuario.UserName,
                    usuario.NormalizedUserName,
                    usuario.PasswordHash,
                    usuario.SecurityStamp,
                    usuario.ConcurrencyStamp
                }, commandType: CommandType.StoredProcedure);

                return usuario.Id;
            }
        }

        public async Task<List<Claim>> ObtenerClaims(IdentityUser user)
        {
            using (var conexion = new SqlConnection(connectionString))
            {
                var claims = await conexion.QueryAsync<ClaimRow>("Usuarios_ObtenerClaims",
                    new { user.Id }, commandType: CommandType.StoredProcedure);
                return claims
                    .Where(claim => !string.IsNullOrWhiteSpace(claim.Type))
                    .Select(claim => new Claim(claim.Type!, claim.Value ?? string.Empty))
                    .ToList();
            }
        }

        public async Task<IList<IdentityUser>> ObtenerUsuariosPorClaim(Claim claim)
        {
            const string sql = """
                SELECT u.*
                FROM Usuarios u
                INNER JOIN UsuariosClaims uc ON uc.UserId = u.Id
                WHERE uc.ClaimType = @Type AND uc.ClaimValue = @Value
                """;

            using var conexion = new SqlConnection(connectionString);
            var usuarios = await conexion.QueryAsync<IdentityUser>(sql, new { claim.Type, claim.Value });
            return usuarios.ToList();
        }

        public async Task AsignarClaims(IdentityUser user, IEnumerable<Claim> claims)
        {
            var sql = @"INSERT INTO UsuariosClaims (UserId, ClaimType, ClaimValue)
                        VALUES (@Id, @Type, @Value)";

            var parametros = claims.Select(x => new { user.Id, x.Type, x.Value });

            using (var conexion = new SqlConnection(connectionString))
            {
                await conexion.ExecuteAsync(sql, parametros);
            }
        }

        public async Task RemoverClaims(IdentityUser user, IEnumerable<Claim> claims)
        {
            var sql = @"DELETE UsuariosClaims WHERE UserId = @Id AND ClaimType = @Type AND ClaimValue = @Value";
            var parametros = claims.Select(x => new { user.Id, x.Type, x.Value });

            using (var conexion = new SqlConnection(connectionString))
            {
                await conexion.ExecuteAsync(sql, parametros);
            }
        }

        public async Task<bool> Actualizar(IdentityUser usuario)
        {
            using var conexion = new SqlConnection(connectionString);

            var rows = await conexion.QuerySingleAsync<int>(
                "Usuarios_Actualizar",
                new
                {
                    usuario.Id,
                    usuario.Email,
                    usuario.NormalizedEmail,
                    usuario.UserName,
                    usuario.NormalizedUserName,
                    usuario.PasswordHash,      // necesario para cambio de contraseña
                    usuario.ConcurrencyStamp,
                    usuario.SecurityStamp
                },
                commandType: CommandType.StoredProcedure);

            return rows == 1;
        }

        public async Task<bool> Eliminar(IdentityUser usuario)
        {
            const string sql = "DELETE Usuarios WHERE Id = @Id";

            using var conexion = new SqlConnection(connectionString);
            var rows = await conexion.ExecuteAsync(sql, new { usuario.Id });
            return rows == 1;
        }

        public async Task<List<IdentityUser>> ListarUsuarios()
        {
            const string sql = "SELECT * FROM Usuarios ORDER BY Email";

            using var conexion = new SqlConnection(connectionString);
            var usuarios = await conexion.QueryAsync<IdentityUser>(sql);
            return usuarios.ToList();
        }

        private sealed class ClaimRow
        {
            public string? Type { get; set; }
            public string? Value { get; set; }
        }

    }
}
