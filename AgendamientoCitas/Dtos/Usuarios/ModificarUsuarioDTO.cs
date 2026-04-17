namespace AgendamientoCitas.Dtos
{
    public class ModificarUsuarioDTO
    {
        public string EmailActual { get; set; } = null!;
        public string NuevoEmail { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string Apellido { get; set; } = null!;
    }
}
