using Microsoft.IdentityModel.Tokens;

namespace AgendamientoCitas.Utilidades;

public static class Llaves
{
    public const string IssuerPropio = "AgendamientoCitas";
    private const string SeccionLlaves = "Authentication:Schemes:Bearer:SigningKeys";
    private const string SeccionLlavesEmisor = "Issuer";
    private const string SeccionLlavesValor = "Value";

    public static IEnumerable<SecurityKey> ObtenerLlave(IConfiguration configuration)
    {
        return ObtenerLlave(configuration, IssuerPropio);
    }

    public static IEnumerable<SecurityKey> ObtenerLlave(IConfiguration configuration, string issuer)
    {
        var signingKey = configuration.GetSection(SeccionLlaves)
            .GetChildren()
            .SingleOrDefault(key => key[SeccionLlavesEmisor] == issuer);

        if (signingKey is not null && signingKey[SeccionLlavesValor] is string keyValue)
        {
            yield return new SymmetricSecurityKey(Convert.FromBase64String(keyValue));
        }
    }

    public static IEnumerable<SecurityKey> ObtenerTodasLasLlaves(IConfiguration configuration)
    {
        var signingKeys = configuration.GetSection(SeccionLlaves).GetChildren();

        foreach (var signingKey in signingKeys)
        {
            if (signingKey[SeccionLlavesValor] is string keyValue)
            {
                yield return new SymmetricSecurityKey(Convert.FromBase64String(keyValue));
            }
        }
    }
}
