using AgendamientoCitas.Models;

namespace AgendamientoCitas.Dtos;

public class IngresoCrearDTO
{
    public int? CitaId { get; set; }

    public int? ClienteId { get; set; }

    public string Concepto { get; set; } = null!;

    public decimal Monto { get; set; }

    public MetodoPago MetodoPago { get; set; }

    public DateTime FechaPago { get; set; }

    public string? Referencia { get; set; }

    public string? Notas { get; set; }
}

public class IngresoModificarDTO : IngresoCrearDTO
{
    public int Id { get; set; }
}

public class IngresoConsultarDTO
{
    public int Id { get; set; }

    public int? CitaId { get; set; }

    public int? ClienteId { get; set; }

    public string? Cliente { get; set; }

    public string Concepto { get; set; } = null!;

    public decimal Monto { get; set; }

    public MetodoPago MetodoPago { get; set; }

    public DateTime FechaPago { get; set; }

    public string? Referencia { get; set; }

    public string? Notas { get; set; }
}

public class IngresoResumenDTO
{
    public decimal Total { get; set; }

    public int Cantidad { get; set; }

    public List<IngresoPorMetodoDTO> PorMetodo { get; set; } = new();
}

public class IngresoPorMetodoDTO
{
    public MetodoPago MetodoPago { get; set; }

    public decimal Total { get; set; }

    public int Cantidad { get; set; }
}
