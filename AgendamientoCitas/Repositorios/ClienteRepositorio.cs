using AgendamientoCitas.Data;
using AgendamientoCitas.Dtos;
using AgendamientoCitas.Models;
using Dapper;

namespace AgendamientoCitas.Repositorios;

public sealed class ClienteRepositorio(SqlConnectionFactory db) : IClienteRepositorio
{
    public async Task<IEnumerable<ClienteConsultarDTO>> ObtenerTodosAsync()
    {
        using var connection = db.CreateConnection();
        const string sql = """
            SELECT Id, Nombres, Apellidos, Identificacion, Telefono, Email, FechaCreacion, Activo
            FROM Clientes
            ORDER BY Apellidos, Nombres;
            """;

        return await connection.QueryAsync<ClienteConsultarDTO>(sql);
    }

    public async Task<ClienteConsultarDTO?> ObtenerPorIdAsync(int id)
    {
        using var connection = db.CreateConnection();
        const string sql = """
            SELECT Id, Nombres, Apellidos, Identificacion, Telefono, Email, FechaCreacion, Activo
            FROM Clientes
            WHERE Id = @Id;
            """;

        return await connection.QuerySingleOrDefaultAsync<ClienteConsultarDTO>(sql, new { Id = id });
    }

    public async Task<ClienteConsultarDTO> CrearAsync(Cliente cliente)
    {
        using var connection = db.CreateConnection();
        const string sql = """
            INSERT INTO Clientes (Nombres, Apellidos, Identificacion, Telefono, Email, FechaCreacion, Activo)
            OUTPUT INSERTED.Id, INSERTED.Nombres, INSERTED.Apellidos, INSERTED.Identificacion,
                   INSERTED.Telefono, INSERTED.Email, INSERTED.FechaCreacion, INSERTED.Activo
            VALUES (@Nombres, @Apellidos, @Identificacion, @Telefono, @Email, SYSUTCDATETIME(), 1);
            """;

        return await connection.QuerySingleAsync<ClienteConsultarDTO>(sql, new
        {
            Nombres = cliente.Nombres.Trim(),
            Apellidos = cliente.Apellidos.Trim(),
            Identificacion = RepositoryHelpers.TrimToNull(cliente.Identificacion),
            Telefono = RepositoryHelpers.TrimToNull(cliente.Telefono),
            Email = RepositoryHelpers.TrimToNull(cliente.Email)
        });
    }

    public async Task<bool> ActualizarAsync(Cliente cliente)
    {
        using var connection = db.CreateConnection();
        const string sql = """
            UPDATE Clientes
            SET Nombres = @Nombres,
                Apellidos = @Apellidos,
                Identificacion = @Identificacion,
                Telefono = @Telefono,
                Email = @Email
            WHERE Id = @Id;
            """;

        var affected = await connection.ExecuteAsync(sql, new
        {
            cliente.Id,
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
        using var connection = db.CreateConnection();
        const string sql = "UPDATE Clientes SET Activo = 0 WHERE Id = @Id;";
        return await connection.ExecuteAsync(sql, new { Id = id }) > 0;
    }

    public async Task<bool> CambiarEstadoAsync(int id, bool activo)
    {
        using var connection = db.CreateConnection();
        const string sql = "UPDATE Clientes SET Activo = @Activo WHERE Id = @Id;";
        return await connection.ExecuteAsync(sql, new { Id = id, Activo = activo }) > 0;
    }

    public async Task<bool> ExisteActivoAsync(int id)
    {
        using var connection = db.CreateConnection();
        const string sql = "SELECT COUNT(1) FROM Clientes WHERE Id = @Id AND Activo = 1;";
        return await connection.ExecuteScalarAsync<int>(sql, new { Id = id }) > 0;
    }

    public async Task<bool> ExisteAsync(int id)
    {
        using var connection = db.CreateConnection();
        const string sql = "SELECT COUNT(1) FROM Clientes WHERE Id = @Id;";
        return await connection.ExecuteScalarAsync<int>(sql, new { Id = id }) > 0;
    }
}
