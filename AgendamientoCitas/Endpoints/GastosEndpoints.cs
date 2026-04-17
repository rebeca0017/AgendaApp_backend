using AgendamientoCitas.Dtos;
using AgendamientoCitas.Models;
using AgendamientoCitas.Repositorios;
using AutoMapper;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AgendamientoCitas.Endpoints;

public static class GastosEndpoints
{
    public static RouteGroupBuilder MapGastos(this RouteGroupBuilder group)
    {
        group.MapGet("/", ObtenerGastos);
        group.MapGet("/{id:int}", ObtenerGastoPorId);
        group.MapGet("/resumen", ObtenerResumenGastos);
        group.MapPost("/", CrearGasto);
        group.MapPut("/{id:int}", ActualizarGasto);
        group.MapDelete("/{id:int}", EliminarGasto);

        return group;
    }

    public static async Task<Ok<IEnumerable<GastoConsultarDTO>>> ObtenerGastos(
        DateTime? desde,
        DateTime? hasta,
        string? categoria,
        IGastoRepositorio repository,
        CancellationToken ct)
    {
        var gastos = await repository.ObtenerTodosAsync(desde, hasta, categoria);
        return TypedResults.Ok(gastos);
    }

    public static async Task<Results<Ok<GastoConsultarDTO>, NotFound>> ObtenerGastoPorId(
        int id,
        IGastoRepositorio repository,
        CancellationToken ct)
    {
        var gasto = await repository.ObtenerPorIdAsync(id);
        return gasto is null ? TypedResults.NotFound() : TypedResults.Ok(gasto);
    }

    public static async Task<Ok<GastoResumenDTO>> ObtenerResumenGastos(
        DateTime? desde,
        DateTime? hasta,
        IGastoRepositorio repository,
        CancellationToken ct)
    {
        var resumen = await repository.ObtenerResumenAsync(desde, hasta);
        return TypedResults.Ok(resumen);
    }

    public static async Task<Results<Created<IdResponseDTO>, BadRequest<string>>> CrearGasto(
        GastoCrearDTO gastoCrearDTO,
        IGastoRepositorio gastos,
        IMapper mapper,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger(typeof(GastosEndpoints).FullName!);
        logger.LogInformation("Creando un nuevo gasto");

        var error = ValidarGasto(gastoCrearDTO);
        if (error is not null)
        {
            return TypedResults.BadRequest(error);
        }

        var gasto = mapper.Map<Gasto>(gastoCrearDTO);
        var id = await gastos.CrearAsync(gasto);

        logger.LogInformation("Gasto creado con exito. Id: {GastoId}", id);
        return TypedResults.Created($"/api/gastos/{id}", new IdResponseDTO { Id = id });
    }

    public static async Task<Results<NoContent, NotFound, BadRequest<string>>> ActualizarGasto(
        int id,
        GastoModificarDTO gastoModificarDTO,
        IGastoRepositorio gastos,
        IMapper mapper,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger(typeof(GastosEndpoints).FullName!);
        logger.LogInformation("Actualizando gasto. Id: {GastoId}", id);

        var error = ValidarGasto(gastoModificarDTO);
        if (error is not null)
        {
            return TypedResults.BadRequest(error);
        }

        gastoModificarDTO.Id = id;
        var gasto = mapper.Map<Gasto>(gastoModificarDTO);
        var updated = await gastos.ActualizarAsync(gasto);

        return updated ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    public static async Task<Results<NoContent, NotFound>> EliminarGasto(
        int id,
        IGastoRepositorio repository,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger(typeof(GastosEndpoints).FullName!);
        logger.LogInformation("Eliminando gasto. Id: {GastoId}", id);

        var deleted = await repository.EliminarAsync(id);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    private static string? ValidarGasto(GastoCrearDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.Concepto))
        {
            return "El concepto es obligatorio.";
        }

        if (string.IsNullOrWhiteSpace(request.Categoria))
        {
            return "La categoria es obligatoria.";
        }

        if (request.Monto <= 0)
        {
            return "El monto debe ser mayor a cero.";
        }

        return null;
    }
}
