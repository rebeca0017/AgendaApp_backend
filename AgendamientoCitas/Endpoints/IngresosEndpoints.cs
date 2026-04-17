using AgendamientoCitas.Dtos;
using AgendamientoCitas.Models;
using AgendamientoCitas.Repositorios;
using AutoMapper;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AgendamientoCitas.Endpoints;

public static class IngresosEndpoints
{
    public static RouteGroupBuilder MapIngresos(this RouteGroupBuilder group)
    {
        group.MapGet("/", ObtenerIngresos);
        group.MapGet("/{id:int}", ObtenerIngresoPorId);
        group.MapGet("/resumen", ObtenerResumenIngresos);
        group.MapPost("/", CrearIngreso);
        group.MapPut("/{id:int}", ActualizarIngreso);
        group.MapDelete("/{id:int}", EliminarIngreso);

        return group;
    }

    public static async Task<Ok<IEnumerable<IngresoConsultarDTO>>> ObtenerIngresos(
        DateTime? desde,
        DateTime? hasta,
        IIngresoRepositorio repository,
        CancellationToken ct)
    {
        var ingresos = await repository.ObtenerTodosAsync(desde, hasta);
        return TypedResults.Ok(ingresos);
    }

    public static async Task<Results<Ok<IngresoConsultarDTO>, NotFound>> ObtenerIngresoPorId(
        int id,
        IIngresoRepositorio repository,
        CancellationToken ct)
    {
        var ingreso = await repository.ObtenerPorIdAsync(id);
        return ingreso is null ? TypedResults.NotFound() : TypedResults.Ok(ingreso);
    }

    public static async Task<Ok<IngresoResumenDTO>> ObtenerResumenIngresos(
        DateTime? desde,
        DateTime? hasta,
        IIngresoRepositorio repository,
        CancellationToken ct)
    {
        var resumen = await repository.ObtenerResumenAsync(desde, hasta);
        return TypedResults.Ok(resumen);
    }

    public static async Task<Results<Created<IdResponseDTO>, BadRequest<string>>> CrearIngreso(
        IngresoCrearDTO ingresoCrearDTO,
        IIngresoRepositorio ingresos,
        ICitaRepositorio citas,
        IClienteRepositorio clientes,
        IServicioRepositorio servicios,
        IMapper mapper,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger(typeof(IngresosEndpoints).FullName!);
        logger.LogInformation("Creando un nuevo ingreso");

        var error = await ValidarIngresoAsync(ingresoCrearDTO, ingresos, citas, clientes, servicios);
        if (error is not null)
        {
            return TypedResults.BadRequest(error);
        }

        var ingreso = mapper.Map<Ingreso>(ingresoCrearDTO);
        var id = await ingresos.CrearAsync(ingreso);

        logger.LogInformation("Ingreso creado con exito. Id: {IngresoId}", id);
        return TypedResults.Created($"/api/ingresos/{id}", new IdResponseDTO { Id = id });
    }

    public static async Task<Results<NoContent, NotFound, BadRequest<string>>> ActualizarIngreso(
        int id,
        IngresoModificarDTO ingresoModificarDTO,
        IIngresoRepositorio ingresos,
        ICitaRepositorio citas,
        IClienteRepositorio clientes,
        IServicioRepositorio servicios,
        IMapper mapper,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger(typeof(IngresosEndpoints).FullName!);
        logger.LogInformation("Actualizando ingreso. Id: {IngresoId}", id);

        var error = await ValidarIngresoAsync(ingresoModificarDTO, ingresos, citas, clientes, servicios, id);
        if (error is not null)
        {
            return TypedResults.BadRequest(error);
        }

        ingresoModificarDTO.Id = id;
        var ingreso = mapper.Map<Ingreso>(ingresoModificarDTO);
        var updated = await ingresos.ActualizarAsync(ingreso);

        return updated ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    public static async Task<Results<NoContent, NotFound>> EliminarIngreso(
        int id,
        IIngresoRepositorio repository,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger(typeof(IngresosEndpoints).FullName!);
        logger.LogInformation("Eliminando ingreso. Id: {IngresoId}", id);

        var deleted = await repository.EliminarAsync(id);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    private static async Task<string?> ValidarIngresoAsync(
        IngresoCrearDTO request,
        IIngresoRepositorio ingresos,
        ICitaRepositorio citas,
        IClienteRepositorio clientes,
        IServicioRepositorio servicios,
        int? ingresoId = null)
    {
        if (string.IsNullOrWhiteSpace(request.Concepto))
        {
            return "El concepto es obligatorio.";
        }

        if (request.Monto <= 0)
        {
            return "El monto debe ser mayor a cero.";
        }

        if (request.CitaId.HasValue && !await citas.ExisteAsync(request.CitaId.Value))
        {
            return "La cita indicada no existe.";
        }

        if (request.CitaId.HasValue)
        {
            var cita = await citas.ObtenerPorIdAsync(request.CitaId.Value);

            if (cita is null)
            {
                return "La cita indicada no existe.";
            }

            if (cita.Estado is not EstadoCita.Confirmada and not EstadoCita.Completada)
            {
                return "Solo se pueden registrar ingresos para citas confirmadas o completadas.";
            }

            if (request.ClienteId.HasValue && request.ClienteId.Value != cita.ClienteId)
            {
                return "La cita indicada no pertenece al cliente seleccionado.";
            }

            var servicio = await servicios.ObtenerPorIdAsync(cita.ServicioId);

            if (servicio is null)
            {
                return "El servicio de la cita indicada no existe.";
            }

            var totalAbonado = await ingresos.ObtenerTotalPorCitaAsync(cita.Id, ingresoId);
            var saldoPendiente = servicio.Precio - totalAbonado;

            if (saldoPendiente <= 0)
            {
                return "La cita indicada ya esta liquidada.";
            }

            if (request.Monto > saldoPendiente)
            {
                return $"El monto supera el saldo pendiente de la cita: {saldoPendiente:C}.";
            }
        }

        if (request.ClienteId.HasValue && !await clientes.ExisteAsync(request.ClienteId.Value))
        {
            return "El cliente indicado no existe.";
        }

        return null;
    }
}
