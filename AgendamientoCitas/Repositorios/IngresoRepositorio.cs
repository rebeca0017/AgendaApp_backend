using System.Text;
using AgendamientoCitas.Data;
using AgendamientoCitas.Dtos;
using AgendamientoCitas.Models;
using AgendamientoCitas.Servicios;
using Dapper;

namespace AgendamientoCitas.Repositorios;

public sealed class IngresoRepositorio(SqlConnectionFactory db, IServicioUsuarios servicioUsuarios) : IIngresoRepositorio
{
    public async Task<IEnumerable<IngresoConsultarDTO>> ObtenerTodosAsync(DateTime? desde, DateTime? hasta)
    {
        var usuarioId = await ObtenerUsuarioIdAsync();
        using var connection = db.CreateConnection();
        var sql = new StringBuilder("""
            SELECT i.Id, i.CitaId, i.ClienteId,
                   CASE WHEN cl.Id IS NULL THEN NULL ELSE CONCAT(cl.Nombres, ' ', cl.Apellidos) END AS Cliente,
                   i.Concepto, i.Monto, i.MetodoPago, i.FechaPago, i.Referencia, i.Notas
            FROM Ingresos i
            LEFT JOIN Clientes cl ON cl.Id = i.ClienteId AND cl.UsuarioId = i.UsuarioId
            WHERE i.UsuarioId = @UsuarioId
            """);
        var parameters = new DynamicParameters();
        parameters.Add("UsuarioId", usuarioId);
        sql.AppendLine();

        AgregarFiltrosFecha(sql, parameters, desde, hasta, "i.FechaPago");
        sql.AppendLine("ORDER BY i.FechaPago DESC;");

        var rows = await connection.QueryAsync<IngresoRow>(sql.ToString(), parameters);
        return rows.Select(ToResponse);
    }

    public async Task<IngresoConsultarDTO?> ObtenerPorIdAsync(int id)
    {
        var usuarioId = await ObtenerUsuarioIdAsync();
        using var connection = db.CreateConnection();
        const string sql = """
            SELECT i.Id, i.CitaId, i.ClienteId,
                   CASE WHEN cl.Id IS NULL THEN NULL ELSE CONCAT(cl.Nombres, ' ', cl.Apellidos) END AS Cliente,
                   i.Concepto, i.Monto, i.MetodoPago, i.FechaPago, i.Referencia, i.Notas
            FROM Ingresos i
            LEFT JOIN Clientes cl ON cl.Id = i.ClienteId AND cl.UsuarioId = i.UsuarioId
            WHERE i.Id = @Id AND i.UsuarioId = @UsuarioId;
            """;

        var row = await connection.QuerySingleOrDefaultAsync<IngresoRow>(sql, new { Id = id, UsuarioId = usuarioId });
        return row is null ? null : ToResponse(row);
    }

    public async Task<IngresoResumenDTO> ObtenerResumenAsync(DateTime? desde, DateTime? hasta)
    {
        var usuarioId = await ObtenerUsuarioIdAsync();
        using var connection = db.CreateConnection();
        var where = new StringBuilder("WHERE UsuarioId = @UsuarioId");
        var parameters = new DynamicParameters();
        parameters.Add("UsuarioId", usuarioId);

        AgregarFiltrosFecha(where, parameters, desde, hasta, "FechaPago");

        var totalSql = $"SELECT COALESCE(SUM(Monto), 0) FROM Ingresos {where};";
        var countSql = $"SELECT COUNT(1) FROM Ingresos {where};";
        var methodSql = $"""
            SELECT MetodoPago, SUM(Monto) AS Total, COUNT(1) AS Cantidad
            FROM Ingresos
            {where}
            GROUP BY MetodoPago;
            """;

        var total = await connection.ExecuteScalarAsync<decimal>(totalSql, parameters);
        var cantidad = await connection.ExecuteScalarAsync<int>(countSql, parameters);
        var rows = await connection.QueryAsync<IngresoPorMetodoRow>(methodSql, parameters);

        return new IngresoResumenDTO
        {
            Total = total,
            Cantidad = cantidad,
            PorMetodo = rows.Select(row => new IngresoPorMetodoDTO
            {
                MetodoPago = RepositoryHelpers.ParseEnum<MetodoPago>(row.MetodoPago),
                Total = row.Total,
                Cantidad = row.Cantidad
            }).ToList()
        };
    }

    public async Task<int> CrearAsync(Ingreso ingreso)
    {
        var usuarioId = await ObtenerUsuarioIdAsync();
        using var connection = db.CreateConnection();
        const string sql = """
            INSERT INTO Ingresos (UsuarioId, CitaId, ClienteId, Concepto, Monto, MetodoPago, FechaPago, Referencia, Notas)
            OUTPUT INSERTED.Id
            VALUES (@UsuarioId, @CitaId, @ClienteId, @Concepto, @Monto, @MetodoPago, @FechaPago, @Referencia, @Notas);
            """;

        return await connection.QuerySingleAsync<int>(sql, ToParameters(ingreso, usuarioId));
    }

    public async Task<bool> ActualizarAsync(Ingreso ingreso)
    {
        var usuarioId = await ObtenerUsuarioIdAsync();
        using var connection = db.CreateConnection();
        const string sql = """
            UPDATE Ingresos
            SET CitaId = @CitaId,
                ClienteId = @ClienteId,
                Concepto = @Concepto,
                Monto = @Monto,
                MetodoPago = @MetodoPago,
                FechaPago = @FechaPago,
                Referencia = @Referencia,
                Notas = @Notas
            WHERE Id = @Id AND UsuarioId = @UsuarioId;
            """;

        return await connection.ExecuteAsync(sql, ToParameters(ingreso, usuarioId)) > 0;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var usuarioId = await ObtenerUsuarioIdAsync();
        using var connection = db.CreateConnection();
        const string sql = "DELETE FROM Ingresos WHERE Id = @Id AND UsuarioId = @UsuarioId;";
        return await connection.ExecuteAsync(sql, new { Id = id, UsuarioId = usuarioId }) > 0;
    }

    public async Task<decimal> ObtenerTotalPorCitaAsync(int citaId, int? excluirIngresoId = null)
    {
        var usuarioId = await ObtenerUsuarioIdAsync();
        using var connection = db.CreateConnection();
        const string sql = """
            SELECT COALESCE(SUM(Monto), 0)
            FROM Ingresos
            WHERE UsuarioId = @UsuarioId
              AND CitaId = @CitaId
              AND (@ExcluirIngresoId IS NULL OR Id <> @ExcluirIngresoId);
            """;

        return await connection.ExecuteScalarAsync<decimal>(sql, new
        {
            UsuarioId = usuarioId,
            CitaId = citaId,
            ExcluirIngresoId = excluirIngresoId
        });
    }

    private async Task<string> ObtenerUsuarioIdAsync()
    {
        var usuario = await servicioUsuarios.ObtenerUsuario();
        return usuario?.Id ?? throw new InvalidOperationException("No se pudo resolver el usuario autenticado.");
    }

    private static object ToParameters(Ingreso ingreso, string usuarioId) => new
    {
        UsuarioId = usuarioId,
        ingreso.Id,
        ingreso.CitaId,
        ingreso.ClienteId,
        Concepto = ingreso.Concepto.Trim(),
        ingreso.Monto,
        MetodoPago = ingreso.MetodoPago.ToString(),
        ingreso.FechaPago,
        Referencia = RepositoryHelpers.TrimToNull(ingreso.Referencia),
        Notas = RepositoryHelpers.TrimToNull(ingreso.Notas)
    };

    private static void AgregarFiltrosFecha(
        StringBuilder sql,
        DynamicParameters parameters,
        DateTime? desde,
        DateTime? hasta,
        string columnName)
    {
        if (desde.HasValue)
        {
            sql.AppendLine($"AND {columnName} >= @Desde");
            parameters.Add("Desde", desde.Value);
        }

        if (hasta.HasValue)
        {
            sql.AppendLine($"AND {columnName} <= @Hasta");
            parameters.Add("Hasta", hasta.Value);
        }
    }

    private static IngresoConsultarDTO ToResponse(IngresoRow row) => new()
    {
        Id = row.Id,
        CitaId = row.CitaId,
        ClienteId = row.ClienteId,
        Cliente = row.Cliente,
        Concepto = row.Concepto,
        Monto = row.Monto,
        MetodoPago = RepositoryHelpers.ParseEnum<MetodoPago>(row.MetodoPago),
        FechaPago = row.FechaPago,
        Referencia = row.Referencia,
        Notas = row.Notas
    };

    private sealed record IngresoRow(
        int Id,
        int? CitaId,
        int? ClienteId,
        string? Cliente,
        string Concepto,
        decimal Monto,
        string MetodoPago,
        DateTime FechaPago,
        string? Referencia,
        string? Notas);

    private sealed record IngresoPorMetodoRow(string MetodoPago, decimal Total, int Cantidad);
}
