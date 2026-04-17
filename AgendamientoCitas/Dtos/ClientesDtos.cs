namespace AgendamientoCitas.Dtos;

public class ClienteCrearDTO
{
    public string Nombres { get; set; } = null!;

    public string Apellidos { get; set; } = null!;

    public string? Identificacion { get; set; }

    public string? Telefono { get; set; }

    public string? Email { get; set; }
}

public class ClienteModificarDTO : ClienteCrearDTO
{
    public int Id { get; set; }
}

public class ClienteConsultarDTO
{
    public int Id { get; set; }

    public string Nombres { get; set; } = null!;

    public string Apellidos { get; set; } = null!;

    public string? Identificacion { get; set; }

    public string? Telefono { get; set; }

    public string? Email { get; set; }

    public DateTime FechaCreacion { get; set; }

    public bool Activo { get; set; }
}

public class ClienteCambiarEstadoDTO
{
    public bool Activo { get; set; }
}
