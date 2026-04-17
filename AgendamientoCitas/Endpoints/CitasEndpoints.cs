using AgendamientoCitas.Dtos;
using AgendamientoCitas.Models;
using AgendamientoCitas.Repositorios;
using AutoMapper;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AgendamientoCitas.Endpoints;

public static class CitasEndpoints
{
    public static RouteGroupBuilder MapCitas(this RouteGroupBuilder group)
    {
        group.MapGet("/", ObtenerCitas);
        group.MapGet("/{id:int}", ObtenerCitaPorId);
        group.MapPost("/", CrearCita);
        group.MapPut("/{id:int}", ActualizarCita);
        group.MapPatch("/{id:int}/estado", CambiarEstadoCita);
        group.MapDelete("/{id:int}", CancelarCita);

        return group;
    }

    public static async Task<Ok<IEnumerable<CitaConsultarDTO>>> ObtenerCitas(
        DateTime? desde,
        DateTime? hasta,
        EstadoCita? estado,
        ICitaRepositorio repository,
        CancellationToken ct)
    {
        var citas = await repository.ObtenerTodosAsync(desde, hasta, estado);
        return TypedResults.Ok(citas);
    }

    public static async Task<Results<Ok<CitaConsultarDTO>, NotFound>> ObtenerCitaPorId(
        int id,
        ICitaRepositorio repository,
        CancellationToken ct)
    {
        var cita = await repository.ObtenerPorIdAsync(id);
        return cita is null ? TypedResults.NotFound() : TypedResults.Ok(cita);
    }

    public static async Task<Results<Created<IdResponseDTO>, BadRequest<string>>> CrearCita(
        CitaCrearDTO citaCrearDTO,
        ICitaRepositorio citas,
        IClienteRepositorio clientes,
        IServicioRepositorio servicios,
        IMapper mapper,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger(typeof(CitasEndpoints).FullName!);
        logger.LogInformation("Creando una nueva cita");

        var validation = await ValidarCitaAsync(citaCrearDTO, citas, clientes, servicios);
        if (validation.Error is not null)
        {
            return TypedResults.BadRequest(validation.Error);
        }

        var cita = mapper.Map<Cita>(citaCrearDTO);
        cita.FechaFin = validation.FechaFin;

        var id = await citas.CrearAsync(cita);
        logger.LogInformation("Cita creada con exito. Id: {CitaId}", id);

        return TypedResults.Created($"/api/citas/{id}", new IdResponseDTO { Id = id });
    }

    public static async Task<Results<NoContent, NotFound, BadRequest<string>>> ActualizarCita(
        int id,
        CitaModificarDTO citaModificarDTO,
        ICitaRepositorio citas,
        IClienteRepositorio clientes,
        IServicioRepositorio servicios,
        IMapper mapper,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger(typeof(CitasEndpoints).FullName!);
        logger.LogInformation("Actualizando cita. Id: {CitaId}", id);

        var citaActual = await citas.ObtenerPorIdAsync(id);
        if (citaActual is null)
        {
            return TypedResults.NotFound();
        }

        if (citaActual.Estado == EstadoCita.Completada)
        {
            return TypedResults.BadRequest("La cita completada ya esta cerrada y no se puede modificar.");
        }

        citaModificarDTO.Id = id;
        var validation = await ValidarCitaAsync(citaModificarDTO, citas, clientes, servicios, id);
        if (validation.Error is not null)
        {
            return TypedResults.BadRequest(validation.Error);
        }

        var cita = mapper.Map<Cita>(citaModificarDTO);
        cita.FechaFin = validation.FechaFin;

        await citas.ActualizarAsync(cita);
        return TypedResults.NoContent();
    }

    public static async Task<Results<NoContent, NotFound, BadRequest<string>>> CambiarEstadoCita(
        int id,
        CitaCambiarEstadoDTO citaCambiarEstadoDTO,
        ICitaRepositorio repository,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger(typeof(CitasEndpoints).FullName!);
        logger.LogInformation("Cambiando estado de cita. Id: {CitaId}", id);

        var citaActual = await repository.ObtenerPorIdAsync(id);
        if (citaActual is null)
        {
            return TypedResults.NotFound();
        }

        if (citaActual.Estado == EstadoCita.Completada && citaCambiarEstadoDTO.Estado != EstadoCita.Completada)
        {
            return TypedResults.BadRequest("La cita completada ya esta cerrada y no se puede cambiar.");
        }

        if (RequiereObservacion(citaCambiarEstadoDTO.Estado) && string.IsNullOrWhiteSpace(citaCambiarEstadoDTO.Observaciones))
        {
            return TypedResults.BadRequest("Debe ingresar la razon en observaciones.");
        }

        var updated = await repository.CambiarEstadoAsync(id, citaCambiarEstadoDTO.Estado, citaCambiarEstadoDTO.Observaciones);
        return updated ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    public static async Task<Results<NoContent, NotFound, BadRequest<string>>> CancelarCita(
        int id,
        ICitaRepositorio repository,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger(typeof(CitasEndpoints).FullName!);
        logger.LogInformation("Cancelando cita. Id: {CitaId}", id);

        var citaActual = await repository.ObtenerPorIdAsync(id);
        if (citaActual is null)
        {
            return TypedResults.NotFound();
        }

        if (citaActual.Estado == EstadoCita.Completada)
        {
            return TypedResults.BadRequest("La cita completada ya esta cerrada y no se puede cancelar.");
        }

        return TypedResults.BadRequest("Debe ingresar la razon en observaciones para cancelar la cita.");
    }

    private static bool RequiereObservacion(EstadoCita estado) => estado is EstadoCita.Cancelada or EstadoCita.NoAsistio;

    private static async Task<(string? Error, DateTime FechaFin)> ValidarCitaAsync(
        CitaCrearDTO request,
        ICitaRepositorio citas,
        IClienteRepositorio clientes,
        IServicioRepositorio servicios,
        int? citaId = null)
    {
        if (request.FechaInicio <= DateTime.Now)
        {
            return ("La fecha de la cita debe ser posterior a la fecha y hora actual.", default);
        }

        if (!await clientes.ExisteActivoAsync(request.ClienteId))
        {
            return ("El cliente indicado no existe o esta inactivo.", default);
        }

        var duracion = await servicios.ObtenerDuracionActivaAsync(request.ServicioId);
        if (!duracion.HasValue)
        {
            return ("El servicio indicado no existe o esta inactivo.", default);
        }

        var fechaFin = request.FechaInicio.AddMinutes(duracion.Value);

        var tieneCruce = await citas.ExisteCruceAsync(request.FechaInicio, fechaFin, citaId);

        return tieneCruce
            ? ("Ya existe una cita en ese horario.", fechaFin)
            : (null, fechaFin);
    }
}
