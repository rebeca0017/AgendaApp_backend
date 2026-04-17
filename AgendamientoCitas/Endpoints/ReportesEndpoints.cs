using AgendamientoCitas.Data;
using AgendamientoCitas.Dtos;
using AgendamientoCitas.Servicios;
using Dapper;

namespace AgendamientoCitas.Endpoints;

public static class ReportesEndpoints
{
    public static RouteGroupBuilder MapReportes(this RouteGroupBuilder group)
    {
        group.MapGet("/resumen-financiero", ResumenFinanciero);
        group.MapGet("/saldos-citas", SaldosCitas);
        group.MapGet("/clientes/{clienteId:int}/historial-financiero", HistorialFinancieroCliente);
        return group;
    }

    private static async Task<IResult> ResumenFinanciero(DateTime? desde, DateTime? hasta, SqlConnectionFactory db, IServicioUsuarios servicioUsuarios)
    {
        var usuarioId = await ObtenerUsuarioIdAsync(servicioUsuarios);
        using var connection = db.CreateConnection();
        var filtrosIngresos = CrearFiltroFecha("FechaPago", desde, hasta, usuarioId);
        var filtrosGastos = CrearFiltroFecha("FechaGasto", desde, hasta, usuarioId);
        var filtrosCitas = CrearFiltroFecha("FechaInicio", desde, hasta, usuarioId);

        var resumen = new ResumenFinancieroDTO
        {
            TotalIngresos = await connection.ExecuteScalarAsync<decimal>($"SELECT COALESCE(SUM(Monto), 0) FROM Ingresos {filtrosIngresos.Sql}", filtrosIngresos.Parametros),
            TotalGastos = await connection.ExecuteScalarAsync<decimal>($"SELECT COALESCE(SUM(Monto), 0) FROM Gastos {filtrosGastos.Sql}", filtrosGastos.Parametros),
            CitasCompletadas = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(1) FROM Citas {filtrosCitas.Sql} AND Estado = 'Completada'", filtrosCitas.Parametros),
            CitasCanceladas = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(1) FROM Citas {filtrosCitas.Sql} AND Estado = 'Cancelada'", filtrosCitas.Parametros),
            CitasNoAsistio = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(1) FROM Citas {filtrosCitas.Sql} AND Estado = 'NoAsistio'", filtrosCitas.Parametros),
            IngresosPorMes = (await connection.QueryAsync<SerieMensualDTO>($"""
                SELECT FORMAT(FechaPago, 'yyyy-MM') AS Mes, SUM(Monto) AS Total
                FROM Ingresos
                {filtrosIngresos.Sql}
                GROUP BY FORMAT(FechaPago, 'yyyy-MM')
                ORDER BY Mes
                """, filtrosIngresos.Parametros)).ToList(),
            GastosPorMes = (await connection.QueryAsync<SerieMensualDTO>($"""
                SELECT FORMAT(FechaGasto, 'yyyy-MM') AS Mes, SUM(Monto) AS Total
                FROM Gastos
                {filtrosGastos.Sql}
                GROUP BY FORMAT(FechaGasto, 'yyyy-MM')
                ORDER BY Mes
                """, filtrosGastos.Parametros)).ToList()
        };

        return Results.Ok(resumen);
    }

    private static async Task<IResult> SaldosCitas(SqlConnectionFactory db, IServicioUsuarios servicioUsuarios)
    {
        var usuarioId = await ObtenerUsuarioIdAsync(servicioUsuarios);
        using var connection = db.CreateConnection();
        var saldos = await ObtenerSaldosAsync(connection, usuarioId);
        return Results.Ok(saldos);
    }

    private static async Task<IResult> HistorialFinancieroCliente(int clienteId, SqlConnectionFactory db, IServicioUsuarios servicioUsuarios)
    {
        var usuarioId = await ObtenerUsuarioIdAsync(servicioUsuarios);
        using var connection = db.CreateConnection();

        var cliente = await connection.QuerySingleOrDefaultAsync<ClienteConsultarDTO>("""
            SELECT Id, Nombres, Apellidos, Identificacion, Telefono, Email, FechaCreacion, Activo
            FROM Clientes
            WHERE Id = @ClienteId AND UsuarioId = @UsuarioId
            """, new { ClienteId = clienteId, UsuarioId = usuarioId });

        if (cliente is null)
        {
            return Results.NotFound();
        }

        var ingresos = (await connection.QueryAsync<IngresoConsultarDTO>("""
            SELECT i.Id, i.CitaId, i.ClienteId, CONCAT(c.Nombres, ' ', c.Apellidos) AS Cliente,
                   i.Concepto, i.Monto, i.MetodoPago, i.FechaPago, i.Referencia, i.Notas
            FROM Ingresos i
            LEFT JOIN Clientes c ON c.Id = i.ClienteId AND c.UsuarioId = i.UsuarioId
            WHERE i.UsuarioId = @UsuarioId
              AND i.ClienteId = @ClienteId
            ORDER BY i.FechaPago DESC
            """, new { ClienteId = clienteId, UsuarioId = usuarioId })).ToList();

        var citas = (await ObtenerSaldosAsync(connection, usuarioId, clienteId)).ToList();

        return Results.Ok(new HistorialFinancieroClienteDTO
        {
            Cliente = cliente,
            Ingresos = ingresos,
            Citas = citas,
            TotalIngresos = ingresos.Sum(ingreso => ingreso.Monto),
            SaldoPendiente = citas.Sum(cita => cita.SaldoPendiente)
        });
    }

    private static Task<IEnumerable<SaldoCitaDTO>> ObtenerSaldosAsync(System.Data.IDbConnection connection, string usuarioId, int? clienteId = null)
    {
        const string sql = """
            SELECT c.Id AS CitaId, c.ClienteId, CONCAT(cl.Nombres, ' ', cl.Apellidos) AS Cliente,
                   s.Nombre AS Servicio, c.FechaInicio, c.Estado AS EstadoCita, s.Precio AS TotalServicio,
                   COALESCE(SUM(i.Monto), 0) AS TotalAbonado
            FROM Citas c
            INNER JOIN Clientes cl ON cl.Id = c.ClienteId AND cl.UsuarioId = c.UsuarioId
            INNER JOIN Servicios s ON s.Id = c.ServicioId AND s.UsuarioId = c.UsuarioId
            LEFT JOIN Ingresos i ON i.CitaId = c.Id AND i.UsuarioId = c.UsuarioId
            WHERE c.UsuarioId = @UsuarioId
              AND c.Estado IN ('Confirmada', 'Completada')
              AND (@ClienteId IS NULL OR c.ClienteId = @ClienteId)
            GROUP BY c.Id, c.ClienteId, cl.Nombres, cl.Apellidos, s.Nombre, c.FechaInicio, c.Estado, s.Precio
            ORDER BY c.FechaInicio DESC
            """;

        return connection.QueryAsync<SaldoCitaDTO>(sql, new { UsuarioId = usuarioId, ClienteId = clienteId });
    }

    private static (string Sql, DynamicParameters Parametros) CrearFiltroFecha(string columna, DateTime? desde, DateTime? hasta, string usuarioId)
    {
        var sql = "WHERE UsuarioId = @UsuarioId";
        var parametros = new DynamicParameters();
        parametros.Add("UsuarioId", usuarioId);

        if (desde.HasValue)
        {
            sql += $" AND {columna} >= @Desde";
            parametros.Add("Desde", desde.Value);
        }

        if (hasta.HasValue)
        {
            sql += $" AND {columna} <= @Hasta";
            parametros.Add("Hasta", hasta.Value);
        }

        return (sql, parametros);
    }

    private static async Task<string> ObtenerUsuarioIdAsync(IServicioUsuarios servicioUsuarios)
    {
        var usuario = await servicioUsuarios.ObtenerUsuario();
        return usuario?.Id ?? throw new InvalidOperationException("No se pudo resolver el usuario autenticado.");
    }
}
