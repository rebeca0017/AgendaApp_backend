namespace AgendamientoCitas.Models;

public sealed class Cliente
{
    public int Id { get; set; }

    public string Nombres { get; set; } = string.Empty;

    public string Apellidos { get; set; } = string.Empty;

    public string? Identificacion { get; set; }

    public string? Telefono { get; set; }

    public string? Email { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public bool Activo { get; set; } = true;

    public ICollection<Cita> Citas { get; set; } = [];

    public ICollection<Ingreso> Ingresos { get; set; } = [];
}
