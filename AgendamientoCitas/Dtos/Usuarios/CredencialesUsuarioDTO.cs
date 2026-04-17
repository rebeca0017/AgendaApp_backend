namespace AgendamientoCitas.Dtos
{
    public class CredencialesUsuarioDTO
    {
        public string user { get; set; } = null!;
        public string pass { get; set; } = null!;
        public string? Nombre { get; set; }
        public string? Apellido { get; set; }
    }
}
