using Microsoft.Extensions.Options;

namespace AgendamientoCitas.Servicios;

public class ValidadorIdentificacion : IValidadorIdentificacion
{
    private static readonly HashSet<int> ProvinciasValidas = Enumerable.Range(1, 24).ToHashSet();
    private readonly HttpClient httpClient;
    private readonly IdentificacionSettings settings;

    public ValidadorIdentificacion(HttpClient httpClient, IOptions<IdentificacionSettings> options)
    {
        this.httpClient = httpClient;
        settings = options.Value;
    }

    public async Task<string?> ValidarCedulaAsync(string? identificacion, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identificacion))
        {
            return null;
        }

        var cedula = identificacion.Trim();

        if (cedula.Length != 10 || cedula.Any(caracter => !char.IsDigit(caracter)))
        {
            return "La identificacion debe tener 10 numeros.";
        }

        if (!EsCedulaEcuatorianaValida(cedula))
        {
            return "La identificacion no es una cedula ecuatoriana valida.";
        }

        if (!settings.ValidarExistenciaExterna)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(settings.Endpoint))
        {
            return "No esta configurada la fuente externa para validar identificaciones.";
        }

        var existe = await ExisteEnFuenteExternaAsync(cedula, cancellationToken);
        return existe ? null : "La identificacion no existe en la fuente externa configurada.";
    }

    private static bool EsCedulaEcuatorianaValida(string cedula)
    {
        var provincia = int.Parse(cedula[..2]);
        var tercerDigito = cedula[2] - '0';

        if (!ProvinciasValidas.Contains(provincia) || tercerDigito >= 6)
        {
            return false;
        }

        var suma = 0;

        for (var i = 0; i < 9; i++)
        {
            var digito = cedula[i] - '0';
            var valor = i % 2 == 0 ? digito * 2 : digito;

            if (valor > 9)
            {
                valor -= 9;
            }

            suma += valor;
        }

        var verificador = suma % 10 == 0 ? 0 : 10 - (suma % 10);
        return verificador == cedula[9] - '0';
    }

    private async Task<bool> ExisteEnFuenteExternaAsync(string cedula, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, ConstruirUrl(cedula));

        if (!string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            request.Headers.TryAddWithoutValidation(settings.ApiKeyHeader, settings.ApiKey);
        }

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(1, settings.TimeoutSeconds)));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
        using var response = await httpClient.SendAsync(request, linked.Token);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("No se pudo validar la identificacion en la fuente externa.");
        }

        var content = await response.Content.ReadAsStringAsync(linked.Token);
        return !string.IsNullOrWhiteSpace(content)
            && !content.Contains("\"error\"", StringComparison.OrdinalIgnoreCase)
            && !content.Contains("no encontrado", StringComparison.OrdinalIgnoreCase);
    }

    private string ConstruirUrl(string cedula)
    {
        var endpoint = settings.Endpoint.Trim();
        return endpoint.Contains("{identificacion}", StringComparison.OrdinalIgnoreCase)
            ? endpoint.Replace("{identificacion}", Uri.EscapeDataString(cedula), StringComparison.OrdinalIgnoreCase)
            : $"{endpoint.TrimEnd('/')}/{Uri.EscapeDataString(cedula)}";
    }
}
