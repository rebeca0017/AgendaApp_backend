using AgendamientoCitas.Data;
using AgendamientoCitas.Dtos;
using AgendamientoCitas.Models;
using AgendamientoCitas.Servicios;
using Dapper;

namespace AgendamientoCitas.Repositorios;

public sealed class ServicioRepositorio(SqlConnectionFactory db, IServicioUsuarios servicioUsuarios) : IServicioRepositorio
{
    public async Task<IEnumerable<ServicioConsultarDTO>> ObtenerTodosAsync()
    {
        var usuarioId = await ObtenerUsuarioIdAsync();
        using var connection = db.CreateConnection();
        const string sql = """
            SELECT Id, Nombre, Descripcion, Precio, DuracionMinutos, Activo
            FROM Servicios
            WHERE UsuarioId = @UsuarioId AND Activo = 1
            ORDER BY Nombre;
            """;

        return await connection.QueryAsync<ServicioConsultarDTO>(sql, new { UsuarioId = usuarioId });
    }

    public async Task<ServicioConsultarDTO?> ObtenerPorIdAsync(int id)
    {
        var usuarioId = await ObtenerUsuarioIdAsync();
        using var connection = db.CreateConnection();
        const string sql = """
            SELECT Id, Nombre, Descripcion, Precio, DuracionMinutos, Activo
            FROM Servicios
            WHERE Id = @Id AND UsuarioId = @UsuarioId;
            """;

        return await connection.QuerySingleOrDefaultAsync<ServicioConsultarDTO>(sql, new { Id = id, UsuarioId = usuarioId });
    }

    public async Task<ServicioConsultarDTO> CrearAsync(Servicio servicio)
    {
        var usuarioId = await ObtenerUsuarioIdAsync();
        using var connection = db.CreateConnection();
        const string sql = """
            INSERT INTO Servicios (UsuarioId, Nombre, Descripcion, Precio, DuracionMinutos, Activo)
            OUTPUT INSERTED.Id, INSERTED.Nombre, INSERTED.Descripcion, INSERTED.Precio,
                   INSERTED.DuracionMinutos, INSERTED.Activo
            VALUES (@UsuarioId, @Nombre, @Descripcion, @Precio, @DuracionMinutos, 1);
            """;

        return await connection.QuerySingleAsync<ServicioConsultarDTO>(sql, new
        {
            UsuarioId = usuarioId,
            Nombre = servicio.Nombre.Trim(),
            Descripcion = RepositoryHelpers.TrimToNull(servicio.Descripcion),
            servicio.Precio,
            servicio.DuracionMinutos
        });
    }

    public async Task<bool> ActualizarAsync(Servicio servicio)
    {
        var usuarioId = await ObtenerUsuarioIdAsync();
        using var connection = db.CreateConnection();
        const string sql = """
            UPDATE Servicios
            SET Nombre = @Nombre,
                Descripcion = @Descripcion,
                Precio = @Precio,
                DuracionMinutos = @DuracionMinutos
            WHERE Id = @Id AND UsuarioId = @UsuarioId;
            """;

        var affected = await connection.ExecuteAsync(sql, new
        {
            servicio.Id,
            UsuarioId = usuarioId,
            Nombre = servicio.Nombre.Trim(),
            Descripcion = RepositoryHelpers.TrimToNull(servicio.Descripcion),
            servicio.Precio,
            servicio.DuracionMinutos
        });

        return affected > 0;
    }

    public async Task<bool> DesactivarAsync(int id)
    {
        var usuarioId = await ObtenerUsuarioIdAsync();
        using var connection = db.CreateConnection();
        const string sql = "UPDATE Servicios SET Activo = 0 WHERE Id = @Id AND UsuarioId = @UsuarioId;";
        return await connection.ExecuteAsync(sql, new { Id = id, UsuarioId = usuarioId }) > 0;
    }

    public async Task<int?> ObtenerDuracionActivaAsync(int id)
    {
        var usuarioId = await ObtenerUsuarioIdAsync();
        using var connection = db.CreateConnection();
        const string sql = """
            SELECT DuracionMinutos
            FROM Servicios
            WHERE Id = @Id AND UsuarioId = @UsuarioId AND Activo = 1;
            """;

        return await connection.QuerySingleOrDefaultAsync<int?>(sql, new { Id = id, UsuarioId = usuarioId });
    }

    private async Task<string> ObtenerUsuarioIdAsync()
    {
        var usuario = await servicioUsuarios.ObtenerUsuario();
        return usuario?.Id ?? throw new InvalidOperationException("No se pudo resolver el usuario autenticado.");
    }
}
