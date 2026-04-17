using AgendamientoCitas.Models;

namespace AgendamientoCitas.Dtos;

public class CitaCrearDTO
{
    public int ClienteId { get; set; }

    public int ServicioId { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    public EstadoCita? Estado { get; set; }

    public string? Motivo { get; set; }

    public string? Observaciones { get; set; }
}

public class CitaModificarDTO : CitaCrearDTO
{
    public int Id { get; set; }
}

public class CitaCambiarEstadoDTO
{
    public EstadoCita Estado { get; set; }

    public string? Observaciones { get; set; }
}

public class CitaConsultarDTO
{
    public int Id { get; set; }

    public int ClienteId { get; set; }

    public string Cliente { get; set; } = null!;

    public int ServicioId { get; set; }

    public string Servicio { get; set; } = null!;

    public decimal ServicioPrecio { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }

    public EstadoCita Estado { get; set; }

    public string? Motivo { get; set; }

    public string? Observaciones { get; set; }

    public DateTime FechaCreacion { get; set; }
}
