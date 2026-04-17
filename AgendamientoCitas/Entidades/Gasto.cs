namespace AgendamientoCitas.Models;

public sealed class Gasto
{
    public int Id { get; set; }

    public string Concepto { get; set; } = string.Empty;

    public string Categoria { get; set; } = string.Empty;

    public decimal Monto { get; set; }

    public MetodoPago MetodoPago { get; set; } = MetodoPago.Efectivo;

    public DateTime FechaGasto { get; set; } = DateTime.UtcNow;

    public string? Referencia { get; set; }

    public string? Notas { get; set; }
}
