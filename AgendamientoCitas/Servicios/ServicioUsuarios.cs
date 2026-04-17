using Microsoft.AspNetCore.Identity;

namespace AgendamientoCitas.Servicios
{
    public class ServicioUsuarios : IServicioUsuarios
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly UserManager<IdentityUser> userManager;

        public ServicioUsuarios(IHttpContextAccessor httpContextAccessor,
            UserManager<IdentityUser> userManager)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.userManager = userManager;
        }

        //obtener el usuario actual logueado, me sirve para mis metodos protegidos que necesitan autorizacion.
        public async Task<IdentityUser?> ObtenerUsuario()
        {
            var userClaim = httpContextAccessor.HttpContext!.User.Claims
                .FirstOrDefault(x => x.Type == "user"); // tu token usa "user"

            if (userClaim is null)
            {
                return null;
            }

            var email = userClaim.Value;
            var normalizedEmail = email.ToUpperInvariant(); // tu tabla guarda NormalizedEmail en mayúsculas

            return await userManager.FindByEmailAsync(normalizedEmail);
        }
    }
}
