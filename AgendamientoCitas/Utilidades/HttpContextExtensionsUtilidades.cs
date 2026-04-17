using Microsoft.IdentityModel.Tokens;


namespace AgendamientoCitas.Utilidades{
    public static class HttpContextExtensionsUtilidades{
        public static T ExtraerValorODefecto<T>(this HttpContext context, string nombreCampo, T valorPorDefecto) where T : IParsable<T>
        {
            var valor = context.Request.Query[nombreCampo].ToString();

            if (string.IsNullOrWhiteSpace(valor))
                return valorPorDefecto;

            return T.Parse(valor!, null);
        }
    }
}
