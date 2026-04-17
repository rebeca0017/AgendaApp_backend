using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace AgendamientoCitas.Repositorios
{
    public interface IRepositorioUsuarios
    {
        Task AsignarClaims(IdentityUser user, IEnumerable<Claim> claims);
        Task<bool> Actualizar(IdentityUser usuario);
        Task<IdentityUser?> BuscarUsuarioPorId(string id);
        Task<IdentityUser?> BuscarUsuarioPorEmail(string normalizedEmail);
        Task<string> Crear(IdentityUser usuario);
        Task<bool> Eliminar(IdentityUser usuario);
        Task<IList<IdentityUser>> ObtenerUsuariosPorClaim(Claim claim);
        Task<List<Claim>> ObtenerClaims(IdentityUser user);
        Task RemoverClaims(IdentityUser user, IEnumerable<Claim> claims);
    }
}
