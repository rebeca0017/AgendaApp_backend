using AgendamientoCitas.Dtos;
using AgendamientoCitas.Models;

namespace AgendamientoCitas.Repositorios;

public interface IServicioRepositorio
{
    Task<IEnumerable<ServicioConsultarDTO>> ObtenerTodosAsync();

    Task<ServicioConsultarDTO?> ObtenerPorIdAsync(int id);

    Task<ServicioConsultarDTO> CrearAsync(Servicio servicio);

    Task<bool> ActualizarAsync(Servicio servicio);

    Task<bool> DesactivarAsync(int id);

    Task<int?> ObtenerDuracionActivaAsync(int id);
}
