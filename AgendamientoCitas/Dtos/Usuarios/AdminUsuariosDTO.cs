namespace AgendamientoCitas.Dtos
{
    public class AdminAccionUsuarioDTO
    {
        public string Email { get; set; } = null!;
    }

    public class UsuarioAdminDTO
    {
        public string Id { get; set; } = null!;
        public string Email { get; set; } = null!;
        public bool EmailConfirmed { get; set; }
        public bool EsAdmin { get; set; }
    }
}
