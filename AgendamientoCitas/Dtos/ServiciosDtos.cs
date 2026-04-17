namespace AgendamientoCitas.Dtos;

public class ServicioCrearDTO
{
    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public decimal Precio { get; set; }

    public int DuracionMinutos { get; set; }
}

public class ServicioModificarDTO : ServicioCrearDTO
{
    public int Id { get; set; }
}

public class ServicioConsultarDTO
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public decimal Precio { get; set; }

    public int DuracionMinutos { get; set; }

    public bool Activo { get; set; }
}
