using AgendamientoCitas.Dtos;
using AgendamientoCitas.Models;

namespace AgendamientoCitas.Repositorios;

public interface IGastoRepositorio
{
    Task<IEnumerable<GastoConsultarDTO>> ObtenerTodosAsync(DateTime? desde, DateTime? hasta, string? categoria);

    Task<GastoConsultarDTO?> ObtenerPorIdAsync(int id);

    Task<GastoResumenDTO> ObtenerResumenAsync(DateTime? desde, DateTime? hasta);

    Task<int> CrearAsync(Gasto gasto);

    Task<bool> ActualizarAsync(Gasto gasto);

    Task<bool> EliminarAsync(int id);
}
