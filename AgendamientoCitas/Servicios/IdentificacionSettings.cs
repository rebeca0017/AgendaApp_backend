namespace AgendamientoCitas.Servicios;

public class IdentificacionSettings
{
    public bool ValidarExistenciaExterna { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiKeyHeader { get; set; } = "X-Credits-Token";
    public int TimeoutSeconds { get; set; } = 8;
}
