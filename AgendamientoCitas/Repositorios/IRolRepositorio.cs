namespace AgendamientoCitas.Repositorios;

public interface IRolRepositorio
{
    Task AsignarRolAsync(string usuarioId, string nombreRol);
    Task<int> ContarUsuariosEnRolAsync(string nombreRol);
    Task<IReadOnlyList<string>> ObtenerRolesUsuarioAsync(string usuarioId);
    Task RemoverRolAsync(string usuarioId, string nombreRol);
    Task<bool> UsuarioTieneRolAsync(string usuarioId, string nombreRol);
}
