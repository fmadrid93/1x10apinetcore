namespace Dtos.Notificacion
{
    public class RegistrarTokenRequest
    {
        public int IdUsuario { get; set; }
        public string Token { get; set; } = "";
        public string? Plataforma { get; set; }
    }
}