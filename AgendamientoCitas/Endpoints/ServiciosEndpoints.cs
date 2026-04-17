using AgendamientoCitas.Dtos;
using AgendamientoCitas.Models;
using AgendamientoCitas.Repositorios;
using AutoMapper;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AgendamientoCitas.Endpoints;

public static class ServiciosEndpoints
{
    public static RouteGroupBuilder MapServicios(this RouteGroupBuilder group)
    {
        group.MapGet("/", ObtenerServicios);
        group.MapGet("/{id:int}", ObtenerServicioPorId);
        group.MapPost("/", CrearServicio);
        group.MapPut("/{id:int}", ActualizarServicio);
        group.MapDelete("/{id:int}", DesactivarServicio);

        return group;
    }

    public static async Task<Ok<IEnumerable<ServicioConsultarDTO>>> ObtenerServicios(
        IServicioRepositorio repository,
        CancellationToken ct)
    {
        var servicios = await repository.ObtenerTodosAsync();
        return TypedResults.Ok(servicios);
    }

    public static async Task<Results<Ok<ServicioConsultarDTO>, NotFound>> ObtenerServicioPorId(
        int id,
        IServicioRepositorio repository,
        CancellationToken ct)
    {
        var servicio = await repository.ObtenerPorIdAsync(id);
        return servicio is null ? TypedResults.NotFound() : TypedResults.Ok(servicio);
    }

    public static async Task<Results<Created<ServicioConsultarDTO>, BadRequest<string>>> CrearServicio(
        ServicioCrearDTO servicioCrearDTO,
        IServicioRepositorio repository,
        IMapper mapper,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger(typeof(ServiciosEndpoints).FullName!);
        logger.LogInformation("Creando un nuevo servicio");

        var error = ValidarServicio(servicioCrearDTO);
        if (error is not null)
        {
            return TypedResults.BadRequest(error);
        }

        var servicio = mapper.Map<Servicio>(servicioCrearDTO);
        var servicioCreado = await repository.CrearAsync(servicio);

        logger.LogInformation("Servicio creado con exito. Id: {ServicioId}", servicioCreado.Id);
        return TypedResults.Created($"/api/servicios/{servicioCreado.Id}", servicioCreado);
    }

    public static async Task<Results<NoContent, NotFound, BadRequest<string>>> ActualizarServicio(
        int id,
        ServicioModificarDTO servicioModificarDTO,
        IServicioRepositorio repository,
        IMapper mapper,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger(typeof(ServiciosEndpoints).FullName!);
        logger.LogInformation("Actualizando servicio. Id: {ServicioId}", id);

        var error = ValidarServicio(servicioModificarDTO);
        if (error is not null)
        {
            return TypedResults.BadRequest(error);
        }

        servicioModificarDTO.Id = id;
        var servicio = mapper.Map<Servicio>(servicioModificarDTO);
        var updated = await repository.ActualizarAsync(servicio);

        return updated ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    public static async Task<Results<NoContent, NotFound>> DesactivarServicio(
        int id,
        IServicioRepositorio repository,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger(typeof(ServiciosEndpoints).FullName!);
        logger.LogInformation("Desactivando servicio. Id: {ServicioId}", id);

        var updated = await repository.DesactivarAsync(id);
        return updated ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    private static string? ValidarServicio(ServicioCrearDTO request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            return "El nombre del servicio es obligatorio.";
        }

        if (request.Precio < 0)
        {
            return "El precio no puede ser negativo.";
        }

        if (request.DuracionMinutos <= 0)
        {
            return "La duracion debe ser mayor a cero minutos.";
        }

        return null;
    }
}
