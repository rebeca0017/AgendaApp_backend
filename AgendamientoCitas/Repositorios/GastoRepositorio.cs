using System.Text;
using AgendamientoCitas.Data;
using AgendamientoCitas.Dtos;
using AgendamientoCitas.Models;
using AgendamientoCitas.Servicios;
using Dapper;

namespace AgendamientoCitas.Repositorios;

public sealed class GastoRepositorio(SqlConnectionFactory db, IServicioUsuarios servicioUsuarios) : IGastoRepositorio
{
    public async Task<IEnumerable<GastoConsultarDTO>> ObtenerTodosAsync(DateTime? desde, DateTime? hasta, string? categoria)
    {
        var usuarioId = await ObtenerUsuarioIdAsync();
        using var connection = db.CreateConnection();
        var sql = new StringBuilder("""
            SELECT Id, Concepto, Categoria, Monto, MetodoPago, FechaGasto, Referencia, Notas
            FROM Gastos
            WHERE UsuarioId = @UsuarioId
            """);
        var parameters = new DynamicParameters();
        parameters.Add("UsuarioId", usuarioId);
        sql.AppendLine();

        AgregarFiltrosFecha(sql, parameters, desde, hasta, "FechaGasto");

        if (!string.IsNullOrWhiteSpace(categoria))
        {
            sql.AppendLine("AND Categoria LIKE @Categoria");
            parameters.Add("Categoria", $"%{categoria.Trim()}%");
        }

        sql.AppendLine("ORDER BY FechaGasto DESC;");

        var rows = await connection.QueryAsync<GastoRow>(sql.ToString(), parameters);
        return rows.Select(ToResponse);
    }

    public async Task<GastoConsultarDTO?> ObtenerPorIdAsync(int id)
    {
        var usuarioId = await ObtenerUsuarioIdAsync();
        using var connection = db.CreateConnection();
        const string sql = """
            SELECT Id, Concepto, Categoria, Monto, MetodoPago, FechaGasto, Referencia, Notas
            FROM Gastos
            WHERE Id = @Id AND UsuarioId = @UsuarioId;
            """;

        var row = await connection.QuerySingleOrDefaultAsync<GastoRow>(sql, new { Id = id, UsuarioId = usuarioId });
        return row is null ? null : ToResponse(row);
    }

    public async Task<GastoResumenDTO> ObtenerResumenAsync(DateTime? desde, DateTime? hasta)
    {
        var usuarioId = await ObtenerUsuarioIdAsync();
        using var connection = db.CreateConnection();
        var where = new StringBuilder("WHERE UsuarioId = @UsuarioId");
        var parameters = new DynamicParameters();
        parameters.Add("UsuarioId", usuarioId);

        AgregarFiltrosFecha(where, parameters, desde, hasta, "FechaGasto");

        var totalSql = $"SELECT COALESCE(SUM(Monto), 0) FROM Gastos {where};";
        var countSql = $"SELECT COUNT(1) FROM Gastos {where};";
        var categorySql = $"""
            SELECT Categoria, SUM(Monto) AS Total, COUNT(1) AS Cantidad
            FROM Gastos
            {where}
            GROUP BY Categoria
            ORDER BY SUM(Monto) DESC;
            """;

        var total = await connection.ExecuteScalarAsync<decimal>(totalSql, parameters);
        var cantidad = await connection.ExecuteScalarAsync<int>(countSql, parameters);
        var rows = await connection.QueryAsync<GastoPorCategoriaRow>(categorySql, parameters);

        return new GastoResumenDTO
        {
            Total = total,
            Cantidad = cantidad,
            PorCategoria = rows.Select(row => new GastoPorCategoriaDTO
            {
                Categoria = row.Categoria,
                Total = row.Total,
                Cantidad = row.Cantidad
            }).ToList()
        };
    }

    public async Task<int> CrearAsync(Gasto gasto)
    {
        var usuarioId = await ObtenerUsuarioIdAsync();
        using var connection = db.CreateConnection();
        const string sql = """
            INSERT INTO Gastos (UsuarioId, Concepto, Categoria, Monto, MetodoPago, FechaGasto, Referencia, Notas)
            OUTPUT INSERTED.Id
            VALUES (@UsuarioId, @Concepto, @Categoria, @Monto, @MetodoPago, @FechaGasto, @Referencia, @Notas);
            """;

        return await connection.QuerySingleAsync<int>(sql, ToParameters(gasto, usuarioId));
    }

    public async Task<bool> ActualizarAsync(Gasto gasto)
    {
        var usuarioId = await ObtenerUsuarioIdAsync();
        using var connection = db.CreateConnection();
        const string sql = """
            UPDATE Gastos
            SET Concepto = @Concepto,
                Categoria = @Categoria,
                Monto = @Monto,
                MetodoPago = @MetodoPago,
                FechaGasto = @FechaGasto,
                Referencia = @Referencia,
                Notas = @Notas
            WHERE Id = @Id AND UsuarioId = @UsuarioId;
            """;

        return await connection.ExecuteAsync(sql, ToParameters(gasto, usuarioId)) > 0;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var usuarioId = await ObtenerUsuarioIdAsync();
        using var connection = db.CreateConnection();
        const string sql = "DELETE FROM Gastos WHERE Id = @Id AND UsuarioId = @UsuarioId;";
        return await connection.ExecuteAsync(sql, new { Id = id, UsuarioId = usuarioId }) > 0;
    }

    private async Task<string> ObtenerUsuarioIdAsync()
    {
        var usuario = await servicioUsuarios.ObtenerUsuario();
        return usuario?.Id ?? throw new InvalidOperationException("No se pudo resolver el usuario autenticado.");
    }

    private static object ToParameters(Gasto gasto, string usuarioId) => new
    {
        UsuarioId = usuarioId,
        gasto.Id,
        Concepto = gasto.Concepto.Trim(),
        Categoria = gasto.Categoria.Trim(),
        gasto.Monto,
        MetodoPago = gasto.MetodoPago.ToString(),
        gasto.FechaGasto,
        Referencia = RepositoryHelpers.TrimToNull(gasto.Referencia),
        Notas = RepositoryHelpers.TrimToNull(gasto.Notas)
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

    private static GastoConsultarDTO ToResponse(GastoRow row) => new()
    {
        Id = row.Id,
        Concepto = row.Concepto,
        Categoria = row.Categoria,
        Monto = row.Monto,
        MetodoPago = RepositoryHelpers.ParseEnum<MetodoPago>(row.MetodoPago),
        FechaGasto = row.FechaGasto,
        Referencia = row.Referencia,
        Notas = row.Notas
    };

    private sealed record GastoRow(
        int Id,
        string Concepto,
        string Categoria,
        decimal Monto,
        string MetodoPago,
        DateTime FechaGasto,
        string? Referencia,
        string? Notas);

    private sealed record GastoPorCategoriaRow(string Categoria, decimal Total, int Cantidad);
}
