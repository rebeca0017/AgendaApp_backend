namespace AgendamientoCitas.Dtos
{
    public class RecuperarPasswordDTO
    {
        public string Email { get; set; } = null!;
        public string Token { get; set; } = null!;
        public string PasswordNueva { get; set; } = null!;
    }
}
