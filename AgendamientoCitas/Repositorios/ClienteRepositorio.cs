using AgendamientoCitas.Data;
using AgendamientoCitas.Dtos;
using AgendamientoCitas.Models;
using AgendamientoCitas.Servicios;
using Dapper;

namespace AgendamientoCitas.Repositorios;

public sealed class ClienteRepositorio(SqlConnectionFactory db, IServicioUsuarios servicioUsuarios) : IClienteRepositorio
{
    public async Task<IEnumerable<ClienteConsultarDTO>> ObtenerTodosAsync()
    {
        var usuarioId = await ObtenerUsuarioIdAsync();
        using var connection = db.CreateConnection();
        const string sql = """
            SELECT Id, Nombres, Apellidos, Identificacion, Telefono, Email, FechaCreacion, Activo
            FROM Clientes
            WHERE UsuarioId = @UsuarioId
            ORDER BY Apellidos, Nombres;
            """;

        return await connection.QueryAsync<ClienteConsultarDTO>(sql, new { UsuarioId = usuarioId });
    }

    public async Task<ClienteConsultarDTO?> ObtenerPorIdAsync(int id)
    {
        var usuarioId = await ObtenerUsuarioIdAsync();
        using var connection = db.CreateConnection();
        const string sql = """
            SELECT Id, Nombres, Apellidos, Identificacion, Telefono, Email, FechaCreacion, Activo
            FROM Clientes
            WHERE Id = @Id AND UsuarioId = @UsuarioId;
            """;

        return await connection.QuerySingleOrDefaultAsync<ClienteConsultarDTO>(sql, new { Id = id, UsuarioId = usuarioId });
    }

    public async Task<ClienteConsultarDTO> CrearAsync(Cliente cliente)
    {
        var usuarioId = await ObtenerUsuarioIdAsync();
        using var connection = db.CreateConnection();
        const string sql = """
            INSERT INTO Clientes (UsuarioId, Nombres, Apellidos, Identificacion, Telefono, Email, FechaCreacion, Activo)
            OUTPUT INSERTED.Id, INSERTED.Nombres, INSERTED.Apellidos, INSERTED.Identificacion,
                   INSERTED.Telefono, INSERTED.Email, INSERTED.FechaCreacion, INSERTED.Activo
            VALUES (@UsuarioId, @Nombres, @Apellidos, @Identificacion, @Telefono, @Email, SYSUTCDATETIME(), 1);
            """;

        return await connection.QuerySingleAsync<ClienteConsultarDTO>(sql, new
        {
            UsuarioId = usuarioId,
            Nombres = cliente.Nombres.Trim(),
            Apellidos = cliente.Apellidos.Trim(),
            Identificacion = RepositoryHelpers.TrimToNull(cliente.Identificacion),
            Telefono = RepositoryHelpers.TrimToNull(cliente.Telefono),
            Email = RepositoryHelpers.TrimToNull(cliente.Email)
        });
    }

    public async Task<bool> ActualizarAsync(Cliente cliente)
    {
        var usuarioId = await ObtenerUsuarioIdAsync();
        using var connection = db.CreateConnection();
        const string sql = """
            UPDATE Clientes
            SET Nombres = @Nombres,
                Apellidos = @Apellidos,
                Identificacion = @Identificacion,
                Telefono = @Telefono,
                Email = @Email
            WHERE Id = @Id AND UsuarioId = @UsuarioId;
            """;

        var affected = await connection.ExecuteAsync(sql, new
        {
            cliente.Id,
            UsuarioId = usuarioId,
            Nombres = cliente.Nombres.Trim(),
            Apellidos = cliente.Apellidos.Trim(),
            Identificacion = RepositoryHelpers.TrimToNull(cliente.Identificacion),
            Telefono = RepositoryHelpers.TrimToNull(cliente.Telefono),
            Email = RepositoryHelpers.TrimToNull(cliente.Email)
        });

        return affected > 0;
    }

    public async Task<bool> DesactivarAsync(int id)
    {
        var usuarioId = await ObtenerUsuarioIdAsync();
        using var connection = db.CreateConnection();
        const string sql = "UPDATE Clientes SET Activo = 0 WHERE Id = @Id AND UsuarioId = @UsuarioId;";
        return await connection.ExecuteAsync(sql, new { Id = id, UsuarioId = usuarioId }) > 0;
    }

    public async Task<bool> CambiarEstadoAsync(int id, bool activo)
    {
        var usuarioId = await ObtenerUsuarioIdAsync();
        using var connection = db.CreateConnection();
        const string sql = "UPDATE Clientes SET Activo = @Activo WHERE Id = @Id AND UsuarioId = @UsuarioId;";
        return await connection.ExecuteAsync(sql, new { Id = id, Activo = activo, UsuarioId = usuarioId }) > 0;
    }

    public async Task<bool> ExisteActivoAsync(int id)
    {
        var usuarioId = await ObtenerUsuarioIdAsync();
        using var connection = db.CreateConnection();
        const string sql = "SELECT COUNT(1) FROM Clientes WHERE Id = @Id AND UsuarioId = @UsuarioId AND Activo = 1;";
        return await connection.ExecuteScalarAsync<int>(sql, new { Id = id, UsuarioId = usuarioId }) > 0;
    }

    public async Task<bool> ExisteAsync(int id)
    {
        var usuarioId = await ObtenerUsuarioIdAsync();
        using var connection = db.CreateConnection();
        const string sql = "SELECT COUNT(1) FROM Clientes WHERE Id = @Id AND UsuarioId = @UsuarioId;";
        return await connection.ExecuteScalarAsync<int>(sql, new { Id = id, UsuarioId = usuarioId }) > 0;
    }

    public async Task<bool> ExisteIdentificacionAsync(string identificacion, int? excluirId = null)
    {
        var usuarioId = await ObtenerUsuarioIdAsync();
        using var connection = db.CreateConnection();
        const string sql = """
            SELECT COUNT(1)
            FROM Clientes
            WHERE UsuarioId = @UsuarioId
              AND Identificacion = @Identificacion
              AND (@ExcluirId IS NULL OR Id <> @ExcluirId);
            """;

        return await connection.ExecuteScalarAsync<int>(sql, new
        {
            UsuarioId = usuarioId,
            Identificacion = identificacion.Trim(),
            ExcluirId = excluirId
        }) > 0;
    }

    private async Task<string> ObtenerUsuarioIdAsync()
    {
        var usuario = await servicioUsuarios.ObtenerUsuario();
        return usuario?.Id ?? throw new InvalidOperationException("No se pudo resolver el usuario autenticado.");
    }
}
