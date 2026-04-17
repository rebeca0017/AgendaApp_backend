using System.Text;
using AgendamientoCitas.Data;
using AgendamientoCitas.Dtos;
using AgendamientoCitas.Models;
using AgendamientoCitas.Servicios;
using Dapper;

namespace AgendamientoCitas.Repositorios;

public sealed class CitaRepositorio(SqlConnectionFactory db, IServicioUsuarios servicioUsuarios) : ICitaRepositorio
{
    public async Task<IEnumerable<CitaConsultarDTO>> ObtenerTodosAsync(DateTime? desde, DateTime? hasta, EstadoCita? estado)
    {
        var usuarioId = await ObtenerUsuarioIdAsync();
        using var connection = db.CreateConnection();
        var sql = new StringBuilder("""
            SELECT c.Id, c.ClienteId, CONCAT(cl.Nombres, ' ', cl.Apellidos) AS Cliente,
                   c.ServicioId, s.Nombre AS Servicio, s.Precio AS ServicioPrecio, c.FechaInicio, c.FechaFin,
                   c.Estado, c.Motivo, c.Observaciones, c.FechaCreacion
            FROM Citas c
            INNER JOIN Clientes cl ON cl.Id = c.ClienteId AND cl.UsuarioId = c.UsuarioId
            INNER JOIN Servicios s ON s.Id = c.ServicioId AND s.UsuarioId = c.UsuarioId
            WHERE c.UsuarioId = @UsuarioId
            """);
        var parameters = new DynamicParameters();
        parameters.Add("UsuarioId", usuarioId);
        sql.AppendLine();

        if (desde.HasValue)
        {
            sql.AppendLine("AND c.FechaInicio >= @Desde");
            parameters.Add("Desde", desde.Value);
        }

        if (hasta.HasValue)
        {
            sql.AppendLine("AND c.FechaInicio <= @Hasta");
            parameters.Add("Hasta", hasta.Value);
        }

        if (estado.HasValue)
        {
            sql.AppendLine("AND c.Estado = @Estado");
            parameters.Add("Estado", estado.Value.ToString());
        }

        sql.AppendLine("ORDER BY c.FechaInicio;");

        var rows = await connection.QueryAsync<CitaRow>(sql.ToString(), parameters);
        return rows.Select(ToResponse);
    }

    public async Task<CitaConsultarDTO?> ObtenerPorIdAsync(int id)
    {
        var usuarioId = await ObtenerUsuarioIdAsync();
        using var connection = db.CreateConnection();
        const string sql = """
            SELECT c.Id, c.ClienteId, CONCAT(cl.Nombres, ' ', cl.Apellidos) AS Cliente,
                   c.ServicioId, s.Nombre AS Servicio, s.Precio AS ServicioPrecio, c.FechaInicio, c.FechaFin,
                   c.Estado, c.Motivo, c.Observaciones, c.FechaCreacion
            FROM Citas c
            INNER JOIN Clientes cl ON cl.Id = c.ClienteId AND cl.UsuarioId = c.UsuarioId
            INNER JOIN Servicios s ON s.Id = c.ServicioId AND s.UsuarioId = c.UsuarioId
            WHERE c.Id = @Id AND c.UsuarioId = @UsuarioId;
            """;

        var row = await connection.QuerySingleOrDefaultAsync<CitaRow>(sql, new { Id = id, UsuarioId = usuarioId });
        return row is null ? null : ToResponse(row);
    }

    public async Task<int> CrearAsync(Cita cita)
    {
        var usuarioId = await ObtenerUsuarioIdAsync();
        using var connection = db.CreateConnection();
        const string sql = """
            INSERT INTO Citas (UsuarioId, ClienteId, ServicioId, FechaInicio, FechaFin, Estado, Motivo, Observaciones, FechaCreacion)
            OUTPUT INSERTED.Id
            VALUES (@UsuarioId, @ClienteId, @ServicioId, @FechaInicio, @FechaFin, @Estado, @Motivo, @Observaciones, SYSUTCDATETIME());
            """;

        return await connection.QuerySingleAsync<int>(sql, new
        {
            UsuarioId = usuarioId,
            cita.ClienteId,
            cita.ServicioId,
            cita.FechaInicio,
            cita.FechaFin,
            Estado = cita.Estado.ToString(),
            Motivo = RepositoryHelpers.TrimToNull(cita.Motivo),
            Observaciones = RepositoryHelpers.TrimToNull(cita.Observaciones)
        });
    }

    public async Task<bool> ActualizarAsync(Cita cita)
    {
        var usuarioId = await ObtenerUsuarioIdAsync();
        using var connection = db.CreateConnection();
        const string sql = """
            UPDATE Citas
            SET ClienteId = @ClienteId,
                ServicioId = @ServicioId,
                FechaInicio = @FechaInicio,
                FechaFin = @FechaFin,
                Estado = @Estado,
                Motivo = @Motivo,
                Observaciones = @Observaciones
            WHERE Id = @Id AND UsuarioId = @UsuarioId;
            """;

        var affected = await connection.ExecuteAsync(sql, new
        {
            cita.Id,
            UsuarioId = usuarioId,
            cita.ClienteId,
            cita.ServicioId,
            cita.FechaInicio,
            cita.FechaFin,
            Estado = cita.Estado.ToString(),
            Motivo = RepositoryHelpers.TrimToNull(cita.Motivo),
            Observaciones = RepositoryHelpers.TrimToNull(cita.Observaciones)
        });

        return affected > 0;
    }

    public async Task<bool> CambiarEstadoAsync(int id, EstadoCita estado, string? observaciones = null)
    {
        var usuarioId = await ObtenerUsuarioIdAsync();
        using var connection = db.CreateConnection();
        const string sql = """
            UPDATE Citas
            SET Estado = @Estado,
                Observaciones = COALESCE(@Observaciones, Observaciones)
            WHERE Id = @Id AND UsuarioId = @UsuarioId;
            """;
        return await connection.ExecuteAsync(sql, new
        {
            Id = id,
            UsuarioId = usuarioId,
            Estado = estado.ToString(),
            Observaciones = RepositoryHelpers.TrimToNull(observaciones)
        }) > 0;
    }

    public async Task<bool> ExisteAsync(int id)
    {
        var usuarioId = await ObtenerUsuarioIdAsync();
        using var connection = db.CreateConnection();
        const string sql = "SELECT COUNT(1) FROM Citas WHERE Id = @Id AND UsuarioId = @UsuarioId;";
        return await connection.ExecuteScalarAsync<int>(sql, new { Id = id, UsuarioId = usuarioId }) > 0;
    }

    public async Task<bool> ExisteCruceAsync(DateTime fechaInicio, DateTime fechaFin, int? citaId = null)
    {
        var usuarioId = await ObtenerUsuarioIdAsync();
        using var connection = db.CreateConnection();
        const string sql = """
            SELECT COUNT(1)
            FROM Citas
            WHERE UsuarioId = @UsuarioId
              AND Id <> COALESCE(@CitaId, 0)
              AND Estado NOT IN ('Cancelada', 'NoAsistio')
              AND FechaInicio = @FechaInicio;
            """;

        return await connection.ExecuteScalarAsync<int>(sql, new
        {
            UsuarioId = usuarioId,
            CitaId = citaId,
            FechaInicio = fechaInicio
        }) > 0;
    }

    private async Task<string> ObtenerUsuarioIdAsync()
    {
        var usuario = await servicioUsuarios.ObtenerUsuario();
        return usuario?.Id ?? throw new InvalidOperationException("No se pudo resolver el usuario autenticado.");
    }

    private static CitaConsultarDTO ToResponse(CitaRow row) => new()
    {
        Id = row.Id,
        ClienteId = row.ClienteId,
        Cliente = row.Cliente,
        ServicioId = row.ServicioId,
        Servicio = row.Servicio,
        ServicioPrecio = row.ServicioPrecio,
        FechaInicio = row.FechaInicio,
        FechaFin = row.FechaFin,
        Estado = RepositoryHelpers.ParseEnum<EstadoCita>(row.Estado),
        Motivo = row.Motivo,
        Observaciones = row.Observaciones,
        FechaCreacion = row.FechaCreacion
    };

    private sealed record CitaRow(
        int Id,
        int ClienteId,
        string Cliente,
        int ServicioId,
        string Servicio,
        decimal ServicioPrecio,
        DateTime FechaInicio,
        DateTime FechaFin,
        string Estado,
        string? Motivo,
        string? Observaciones,
        DateTime FechaCreacion);
}
