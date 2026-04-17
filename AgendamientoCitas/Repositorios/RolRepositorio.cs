using AgendamientoCitas.Data;
using Dapper;

namespace AgendamientoCitas.Repositorios;

public sealed class RolRepositorio(SqlConnectionFactory db) : IRolRepositorio
{
    public async Task AsignarRolAsync(string usuarioId, string nombreRol)
    {
        using var connection = db.CreateConnection();
        const string sql = """
            DECLARE @RoleId nvarchar(450);

            SELECT @RoleId = Id
            FROM dbo.Roles
            WHERE NormalizedName = UPPER(@NombreRol);

            IF @RoleId IS NULL
            BEGIN
                SET @RoleId = CONVERT(nvarchar(450), NEWID());

                INSERT INTO dbo.Roles (Id, Name, NormalizedName, ConcurrencyStamp)
                VALUES (@RoleId, @NombreRol, UPPER(@NombreRol), CONVERT(nvarchar(450), NEWID()));
            END;

            IF NOT EXISTS (
                SELECT 1
                FROM dbo.UsuariosRoles
                WHERE UserId = @UsuarioId AND RoleId = @RoleId
            )
            BEGIN
                INSERT INTO dbo.UsuariosRoles (UserId, RoleId)
                VALUES (@UsuarioId, @RoleId);
            END;
            """;

        await connection.ExecuteAsync(sql, new { UsuarioId = usuarioId, NombreRol = nombreRol });
    }

    public async Task<IReadOnlyList<string>> ObtenerRolesUsuarioAsync(string usuarioId)
    {
        using var connection = db.CreateConnection();
        const string sql = """
            SELECT r.Name
            FROM dbo.Roles r
            INNER JOIN dbo.UsuariosRoles ur ON ur.RoleId = r.Id
            WHERE ur.UserId = @UsuarioId
            ORDER BY r.Name;
            """;

        var roles = await connection.QueryAsync<string>(sql, new { UsuarioId = usuarioId });
        return roles.ToList();
    }

    public async Task RemoverRolAsync(string usuarioId, string nombreRol)
    {
        using var connection = db.CreateConnection();
        const string sql = """
            DELETE ur
            FROM dbo.UsuariosRoles ur
            INNER JOIN dbo.Roles r ON r.Id = ur.RoleId
            WHERE ur.UserId = @UsuarioId
              AND r.NormalizedName = UPPER(@NombreRol);
            """;

        await connection.ExecuteAsync(sql, new { UsuarioId = usuarioId, NombreRol = nombreRol });
    }

    public async Task<int> ContarUsuariosEnRolAsync(string nombreRol)
    {
        using var connection = db.CreateConnection();
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.UsuariosRoles ur
            INNER JOIN dbo.Roles r ON r.Id = ur.RoleId
            WHERE r.NormalizedName = UPPER(@NombreRol);
            """;

        return await connection.ExecuteScalarAsync<int>(sql, new { NombreRol = nombreRol });
    }

    public async Task<bool> UsuarioTieneRolAsync(string usuarioId, string nombreRol)
    {
        using var connection = db.CreateConnection();
        const string sql = """
            SELECT COUNT(1)
            FROM dbo.Roles r
            INNER JOIN dbo.UsuariosRoles ur ON ur.RoleId = r.Id
            WHERE ur.UserId = @UsuarioId
              AND r.NormalizedName = UPPER(@NombreRol);
            """;

        return await connection.ExecuteScalarAsync<int>(sql, new { UsuarioId = usuarioId, NombreRol = nombreRol }) > 0;
    }
}
