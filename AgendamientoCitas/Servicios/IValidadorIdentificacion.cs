namespace AgendamientoCitas.Servicios;

public interface IValidadorIdentificacion
{
    Task<string?> ValidarCedulaAsync(string? identificacion, CancellationToken cancellationToken = default);
}
