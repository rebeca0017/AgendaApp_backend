using AgendamientoCitas.Models;

namespace AgendamientoCitas.Dtos;

public class GastoCrearDTO
{
    public string Concepto { get; set; } = null!;

    public string Categoria { get; set; } = null!;

    public decimal Monto { get; set; }

    public MetodoPago MetodoPago { get; set; }

    public DateTime FechaGasto { get; set; }

    public string? Referencia { get; set; }

    public string? Notas { get; set; }
}

public class GastoModificarDTO : GastoCrearDTO
{
    public int Id { get; set; }
}

public class GastoConsultarDTO
{
    public int Id { get; set; }

    public string Concepto { get; set; } = null!;

    public string Categoria { get; set; } = null!;

    public decimal Monto { get; set; }

    public MetodoPago MetodoPago { get; set; }

    public DateTime FechaGasto { get; set; }

    public string? Referencia { get; set; }

    public string? Notas { get; set; }
}

public class GastoResumenDTO
{
    public decimal Total { get; set; }

    public int Cantidad { get; set; }

    public List<GastoPorCategoriaDTO> PorCategoria { get; set; } = new();
}

public class GastoPorCategoriaDTO
{
    public string Categoria { get; set; } = null!;

    public decimal Total { get; set; }

    public int Cantidad { get; set; }
}
