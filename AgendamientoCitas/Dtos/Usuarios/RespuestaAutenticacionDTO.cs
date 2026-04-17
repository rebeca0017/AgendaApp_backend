namespace AgendamientoCitas.Dtos
{
    public class RespuestaAutenticacionDTO
    {
        public string Token { get; set; } = null!;
        public DateTime Expiracion { get; set; }
        public string Email { get; set; } = null!;
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public bool EsAdmin { get; set; }
        public bool DebeCambiarPassword { get; set; }
    }
}
