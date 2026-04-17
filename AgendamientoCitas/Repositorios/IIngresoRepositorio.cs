using AgendamientoCitas.Dtos;
using AgendamientoCitas.Models;

namespace AgendamientoCitas.Repositorios;

public interface IIngresoRepositorio
{
    Task<IEnumerable<IngresoConsultarDTO>> ObtenerTodosAsync(DateTime? desde, DateTime? hasta);

    Task<IngresoConsultarDTO?> ObtenerPorIdAsync(int id);

    Task<IngresoResumenDTO> ObtenerResumenAsync(DateTime? desde, DateTime? hasta);

    Task<int> CrearAsync(Ingreso ingreso);

    Task<bool> ActualizarAsync(Ingreso ingreso);

    Task<bool> EliminarAsync(int id);
}
