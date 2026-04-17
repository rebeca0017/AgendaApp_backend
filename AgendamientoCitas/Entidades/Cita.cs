namespace AgendamientoCitas.Models;

public sealed class Cita
{
    public int Id { get; set; }

    public int ClienteId { get; set; }

    public Cliente Cliente { get; set; } = null!;

    public int ServicioId { get; set; }

    public Servicio Servicio { get; set; } = null!;

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }

    public EstadoCita Estado { get; set; } = EstadoCita.Programada;

    public string? Motivo { get; set; }

    public string? Observaciones { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public ICollection<Ingreso> Ingresos { get; set; } = [];
}
