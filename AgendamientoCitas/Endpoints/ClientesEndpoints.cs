using AgendamientoCitas.Dtos;
using AgendamientoCitas.Models;
using AgendamientoCitas.Repositorios;
using AgendamientoCitas.Servicios;
using AutoMapper;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AgendamientoCitas.Endpoints;

public static class ClientesEndpoints
{
    public static RouteGroupBuilder MapClientes(this RouteGroupBuilder group)
    {
        group.MapGet("/", ObtenerClientes);
        group.MapGet("/{id:int}", ObtenerClientePorId);
        group.MapPost("/", CrearCliente);
        group.MapPut("/{id:int}", ActualizarCliente);
        group.MapPatch("/{id:int}/estado", CambiarEstadoCliente);
        group.MapDelete("/{id:int}", DesactivarCliente);

        return group;
    }

    public static async Task<Ok<IEnumerable<ClienteConsultarDTO>>> ObtenerClientes(
        IClienteRepositorio repository,
        CancellationToken ct)
    {
        var clientes = await repository.ObtenerTodosAsync();
        return TypedResults.Ok(clientes);
    }

    public static async Task<Results<Ok<ClienteConsultarDTO>, NotFound>> ObtenerClientePorId(
        int id,
        IClienteRepositorio repository,
        CancellationToken ct)
    {
        var cliente = await repository.ObtenerPorIdAsync(id);
        return cliente is null ? TypedResults.NotFound() : TypedResults.Ok(cliente);
    }

    public static async Task<Results<Created<ClienteConsultarDTO>, BadRequest<string>>> CrearCliente(
        ClienteCrearDTO clienteCrearDTO,
        IClienteRepositorio repository,
        IValidadorIdentificacion validadorIdentificacion,
        IMapper mapper,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger(typeof(ClientesEndpoints).FullName!);
        logger.LogInformation("Creando un nuevo cliente");

        var error = await ValidarCliente(clienteCrearDTO, repository, validadorIdentificacion, null, ct);
        if (error is not null)
        {
            return TypedResults.BadRequest(error);
        }

        var cliente = mapper.Map<Cliente>(clienteCrearDTO);
        var clienteCreado = await repository.CrearAsync(cliente);

        logger.LogInformation("Cliente creado con exito. Id: {ClienteId}", clienteCreado.Id);
        return TypedResults.Created($"/api/clientes/{clienteCreado.Id}", clienteCreado);
    }

    public static async Task<Results<NoContent, NotFound, BadRequest<string>>> ActualizarCliente(
        int id,
        ClienteModificarDTO clienteModificarDTO,
        IClienteRepositorio repository,
        IValidadorIdentificacion validadorIdentificacion,
        IMapper mapper,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger(typeof(ClientesEndpoints).FullName!);
        logger.LogInformation("Actualizando cliente. Id: {ClienteId}", id);

        var error = await ValidarCliente(clienteModificarDTO, repository, validadorIdentificacion, id, ct);
        if (error is not null)
        {
            return TypedResults.BadRequest(error);
        }

        clienteModificarDTO.Id = id;
        var cliente = mapper.Map<Cliente>(clienteModificarDTO);
        var updated = await repository.ActualizarAsync(cliente);

        return updated ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    public static async Task<Results<NoContent, NotFound>> DesactivarCliente(
        int id,
        IClienteRepositorio repository,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger(typeof(ClientesEndpoints).FullName!);
        logger.LogInformation("Desactivando cliente. Id: {ClienteId}", id);

        var updated = await repository.DesactivarAsync(id);
        return updated ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    public static async Task<Results<NoContent, NotFound>> CambiarEstadoCliente(
        int id,
        ClienteCambiarEstadoDTO clienteCambiarEstadoDTO,
        IClienteRepositorio repository,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var logger = loggerFactory.CreateLogger(typeof(ClientesEndpoints).FullName!);
        logger.LogInformation("Cambiando estado de cliente. Id: {ClienteId}", id);

        var updated = await repository.CambiarEstadoAsync(id, clienteCambiarEstadoDTO.Activo);
        return updated ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    private static async Task<string?> ValidarCliente(
        ClienteCrearDTO request,
        IClienteRepositorio repository,
        IValidadorIdentificacion validadorIdentificacion,
        int? clienteId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Nombres) || string.IsNullOrWhiteSpace(request.Apellidos))
        {
            return "Los nombres y apellidos son obligatorios.";
        }

        var errorIdentificacion = await validadorIdentificacion.ValidarCedulaAsync(request.Identificacion, ct);

        if (errorIdentificacion is not null)
        {
            return errorIdentificacion;
        }

        if (!string.IsNullOrWhiteSpace(request.Identificacion)
            && await repository.ExisteIdentificacionAsync(request.Identificacion.Trim(), clienteId))
        {
            return "Esa identificacion ya existe.";
        }

        return null;
    }
}
