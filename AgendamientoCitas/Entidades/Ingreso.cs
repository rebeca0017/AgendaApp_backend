namespace AgendamientoCitas.Models;

public sealed class Ingreso
{
    public int Id { get; set; }

    public int? CitaId { get; set; }

    public Cita? Cita { get; set; }

    public int? ClienteId { get; set; }

    public Cliente? Cliente { get; set; }

    public string Concepto { get; set; } = string.Empty;

    public decimal Monto { get; set; }

    public MetodoPago MetodoPago { get; set; } = MetodoPago.Efectivo;

    public DateTime FechaPago { get; set; } = DateTime.UtcNow;

    public string? Referencia { get; set; }

    public string? Notas { get; set; }
}
