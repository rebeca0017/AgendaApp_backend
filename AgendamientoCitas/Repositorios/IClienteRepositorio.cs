using AgendamientoCitas.Dtos;
using AgendamientoCitas.Models;

namespace AgendamientoCitas.Repositorios;

public interface IClienteRepositorio
{
    Task<IEnumerable<ClienteConsultarDTO>> ObtenerTodosAsync();

    Task<ClienteConsultarDTO?> ObtenerPorIdAsync(int id);

    Task<ClienteConsultarDTO> CrearAsync(Cliente cliente);

    Task<bool> ActualizarAsync(Cliente cliente);

    Task<bool> DesactivarAsync(int id);

    Task<bool> CambiarEstadoAsync(int id, bool activo);

    Task<bool> ExisteActivoAsync(int id);

    Task<bool> ExisteAsync(int id);

    Task<bool> ExisteIdentificacionAsync(string identificacion, int? excluirId = null);
}
