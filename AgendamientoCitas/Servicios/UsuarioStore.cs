using Microsoft.AspNetCore.Identity;
using AgendamientoCitas.Repositorios;
using System.Security.Claims;

namespace AgendamientoCitas.Servicios
{
   
    public class UsuarioStore : IUserStore<IdentityUser>, IUserEmailStore<IdentityUser>,
        IUserPasswordStore<IdentityUser>, IUserClaimStore<IdentityUser>, IUserSecurityStampStore<IdentityUser>
    {
        private readonly IRepositorioUsuarios repositorioUsuarios;

        public UsuarioStore(IRepositorioUsuarios repositorioUsuarios)
        {
            this.repositorioUsuarios = repositorioUsuarios;
        }

        public async Task AddClaimsAsync(IdentityUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
        {
            await repositorioUsuarios.AsignarClaims(user, claims);
        }

        public async Task<IdentityResult> CreateAsync(IdentityUser user,
            CancellationToken cancellationToken)
        {
            user.Id = await repositorioUsuarios.Crear(user);
            return IdentityResult.Success;
        }
         
        public async Task<IdentityResult> DeleteAsync(IdentityUser user, CancellationToken cancellationToken)
        {
            var ok = await repositorioUsuarios.Eliminar(user);

            return ok
                ? IdentityResult.Success
                : IdentityResult.Failed(new IdentityError { Description = "No se eliminó el usuario" });
        }

        public void Dispose()
        {

        }

        public async Task<IdentityUser?> FindByEmailAsync(string normalizedEmail,
            CancellationToken cancellationToken)
        {
            return await repositorioUsuarios.BuscarUsuarioPorEmail(normalizedEmail);
        }

        public async Task<IdentityUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            return await repositorioUsuarios.BuscarUsuarioPorId(userId);
        }

        public async Task<IdentityUser?> FindByNameAsync(string normalizedUserName,
            CancellationToken cancellationToken)
        {
            return await repositorioUsuarios.BuscarUsuarioPorEmail(normalizedUserName);
        }

        public async Task<IList<Claim>> GetClaimsAsync(IdentityUser user, CancellationToken cancellationToken)
        {
            return await repositorioUsuarios.ObtenerClaims(user);
        }

        public Task<string?> GetEmailAsync(IdentityUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Email);
        }

        public Task<bool> GetEmailConfirmedAsync(IdentityUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.EmailConfirmed);
        }

        public Task<string?> GetNormalizedEmailAsync(IdentityUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.NormalizedEmail);
        }

        public Task<string?> GetNormalizedUserNameAsync(IdentityUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.NormalizedUserName);
        }

        public Task<string?> GetPasswordHashAsync(IdentityUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.PasswordHash);
        }

        public Task<string?> GetSecurityStampAsync(IdentityUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.SecurityStamp);
        }

        public Task<string> GetUserIdAsync(IdentityUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Id);
        }

        public Task<string?> GetUserNameAsync(IdentityUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Email);
        }

        public async Task<IList<IdentityUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken)
        {
            return await repositorioUsuarios.ObtenerUsuariosPorClaim(claim);
        }

        public Task<bool> HasPasswordAsync(IdentityUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));
        }

        public async Task RemoveClaimsAsync(IdentityUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
        {
            await repositorioUsuarios.RemoverClaims(user, claims);
        }

        public async Task ReplaceClaimAsync(IdentityUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken)
        {
            await repositorioUsuarios.RemoverClaims(user, [claim]);
            await repositorioUsuarios.AsignarClaims(user, [newClaim]);
        }

        public Task SetEmailAsync(IdentityUser user, string? email, CancellationToken cancellationToken)
        {
            user.Email = email;
            return Task.CompletedTask;
        }

        public Task SetEmailConfirmedAsync(IdentityUser user, bool confirmed, CancellationToken cancellationToken)
        {
            user.EmailConfirmed = confirmed;
            return Task.CompletedTask;
        }

        public Task SetNormalizedEmailAsync(IdentityUser user, string? normalizedEmail, CancellationToken cancellationToken)
        {
            user.NormalizedEmail = normalizedEmail;
            return Task.CompletedTask;
        }

        public Task SetNormalizedUserNameAsync(IdentityUser user, string? normalizedName, CancellationToken cancellationToken)
        {
            user.NormalizedUserName = normalizedName;
            return Task.CompletedTask;
        }

        public Task SetPasswordHashAsync(IdentityUser user, string? passwordHash, CancellationToken cancellationToken)
        {
            user.PasswordHash = passwordHash;
            return Task.CompletedTask;
        }

        public Task SetSecurityStampAsync(IdentityUser user, string stamp, CancellationToken cancellationToken)
        {
            user.SecurityStamp = stamp;
            return Task.CompletedTask;
        }

        public Task SetUserNameAsync(IdentityUser user, string? userName, CancellationToken cancellationToken)
        {
            user.UserName = userName;
            return Task.CompletedTask;
        }

        public async Task<IdentityResult> UpdateAsync(IdentityUser user, CancellationToken cancellationToken)
        {
            user.ConcurrencyStamp = Guid.NewGuid().ToString();
            user.SecurityStamp ??= Guid.NewGuid().ToString();

            var ok = await repositorioUsuarios.Actualizar(user);

            return ok
                ? IdentityResult.Success
                : IdentityResult.Failed(new IdentityError { Description = "No se actualizó el usuario" });
        }

    }
}
