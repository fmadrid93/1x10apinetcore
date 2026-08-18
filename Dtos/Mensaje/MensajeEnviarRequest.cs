namespace Dtos.Mensaje
{
    public class MensajeEnviarRequest
    {
        public int IdUsuarioEmisor { get; set; }
        public int IdUsuarioDestino { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Contenido { get; set; } = string.Empty;
    }
}