namespace AgendamientoCitas.Servicios
{
    public interface IServicioEmail
    {
        Task EnviarRecuperacionPasswordAsync(string destinatario, string token, CancellationToken cancellationToken = default);
        Task EnviarPasswordTemporalAsync(string destinatario, string passwordTemporal, CancellationToken cancellationToken = default);
    }
}
