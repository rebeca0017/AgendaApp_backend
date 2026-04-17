using AgendamientoCitas.Data;
using AgendamientoCitas.Dtos;
using AgendamientoCitas.Models;
using Dapper;

namespace AgendamientoCitas.Repositorios;

public sealed class ServicioRepositorio(SqlConnectionFactory db) : IServicioRepositorio
{
    public async Task<IEnumerable<ServicioConsultarDTO>> ObtenerTodosAsync()
    {
        using var connection = db.CreateConnection();
        const string sql = """
            SELECT Id, Nombre, Descripcion, Precio, DuracionMinutos, Activo
            FROM Servicios
            WHERE Activo = 1
            ORDER BY Nombre;
            """;

        return await connection.QueryAsync<ServicioConsultarDTO>(sql);
    }

    public async Task<ServicioConsultarDTO?> ObtenerPorIdAsync(int id)
    {
        using var connection = db.CreateConnection();
        const string sql = """
            SELECT Id, Nombre, Descripcion, Precio, DuracionMinutos, Activo
            FROM Servicios
            WHERE Id = @Id;
            """;

        return await connection.QuerySingleOrDefaultAsync<ServicioConsultarDTO>(sql, new { Id = id });
    }

    public async Task<ServicioConsultarDTO> CrearAsync(Servicio servicio)
    {
        using var connection = db.CreateConnection();
        const string sql = """
            INSERT INTO Servicios (Nombre, Descripcion, Precio, DuracionMinutos, Activo)
            OUTPUT INSERTED.Id, INSERTED.Nombre, INSERTED.Descripcion, INSERTED.Precio,
                   INSERTED.DuracionMinutos, INSERTED.Activo
            VALUES (@Nombre, @Descripcion, @Precio, @DuracionMinutos, 1);
            """;

        return await connection.QuerySingleAsync<ServicioConsultarDTO>(sql, new
        {
            Nombre = servicio.Nombre.Trim(),
            Descripcion = RepositoryHelpers.TrimToNull(servicio.Descripcion),
            servicio.Precio,
            servicio.DuracionMinutos
        });
    }

    public async Task<bool> ActualizarAsync(Servicio servicio)
    {
        using var connection = db.CreateConnection();
        const string sql = """
            UPDATE Servicios
            SET Nombre = @Nombre,
                Descripcion = @Descripcion,
                Precio = @Precio,
                DuracionMinutos = @DuracionMinutos
            WHERE Id = @Id;
            """;

        var affected = await connection.ExecuteAsync(sql, new
        {
            servicio.Id,
            Nombre = servicio.Nombre.Trim(),
            Descripcion = RepositoryHelpers.TrimToNull(servicio.Descripcion),
            servicio.Precio,
            servicio.DuracionMinutos
        });

        return affected > 0;
    }

    public async Task<bool> DesactivarAsync(int id)
    {
        using var connection = db.CreateConnection();
        const string sql = "UPDATE Servicios SET Activo = 0 WHERE Id = @Id;";
        return await connection.ExecuteAsync(sql, new { Id = id }) > 0;
    }

    public async Task<int?> ObtenerDuracionActivaAsync(int id)
    {
        using var connection = db.CreateConnection();
        const string sql = """
            SELECT DuracionMinutos
            FROM Servicios
            WHERE Id = @Id AND Activo = 1;
            """;

        return await connection.QuerySingleOrDefaultAsync<int?>(sql, new { Id = id });
    }
}
