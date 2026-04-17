using AgendamientoCitas.Dtos;
using AgendamientoCitas.Models;

namespace AgendamientoCitas.Repositorios;

public interface ICitaRepositorio
{
    Task<IEnumerable<CitaConsultarDTO>> ObtenerTodosAsync(DateTime? desde, DateTime? hasta, EstadoCita? estado);

    Task<CitaConsultarDTO?> ObtenerPorIdAsync(int id);

    Task<int> CrearAsync(Cita cita);

    Task<bool> ActualizarAsync(Cita cita);

    Task<bool> CambiarEstadoAsync(int id, EstadoCita estado, string? observaciones = null);

    Task<bool> ExisteAsync(int id);

    Task<bool> ExisteCruceAsync(DateTime fechaInicio, DateTime fechaFin, int? citaId = null);
}
