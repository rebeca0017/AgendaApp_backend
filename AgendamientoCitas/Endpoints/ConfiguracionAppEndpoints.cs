using AgendamientoCitas.Data;
using AgendamientoCitas.Dtos;
using Dapper;

namespace AgendamientoCitas.Endpoints;

public static class ConfiguracionAppEndpoints
{
    public static RouteGroupBuilder MapConfiguracionApp(this RouteGroupBuilder group)
    {
        group.MapGet("/", Obtener);
        group.MapPut("/", Actualizar).RequireAuthorization("Admin");
        return group;
    }

    private static async Task<IResult> Obtener(SqlConnectionFactory db)
    {
        using var connection = db.CreateConnection();
        const string sql = """
            SELECT TOP 1 NombreApp, Logo
            FROM ConfiguracionApp
            ORDER BY Id;
            """;

        var configuracion = await connection.QuerySingleOrDefaultAsync<ConfiguracionAppDTO>(sql);
        return Results.Ok(configuracion ?? new ConfiguracionAppDTO());
    }

    private static async Task<IResult> Actualizar(ConfiguracionAppDTO dto, SqlConnectionFactory db)
    {
        var nombreApp = string.IsNullOrWhiteSpace(dto.NombreApp) ? "Mi Agenda" : dto.NombreApp.Trim();

        using var connection = db.CreateConnection();
        const string sql = """
            UPDATE ConfiguracionApp
            SET NombreApp = @NombreApp,
                Logo = @Logo,
                FechaActualizacion = SYSUTCDATETIME()
            WHERE Id = 1;
            """;

        await connection.ExecuteAsync(sql, new
        {
            NombreApp = nombreApp,
            Logo = string.IsNullOrWhiteSpace(dto.Logo) ? null : dto.Logo
        });

        return Results.Ok(new ConfiguracionAppDTO { NombreApp = nombreApp, Logo = dto.Logo });
    }
}
