using Microsoft.AspNetCore.Identity;

namespace AgendamientoCitas.Servicios
{
    public interface IServicioUsuarios
    {
        Task<IdentityUser?> ObtenerUsuario();
    }
}