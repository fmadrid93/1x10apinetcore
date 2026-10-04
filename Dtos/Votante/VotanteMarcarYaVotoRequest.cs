namespace Dtos.Votante
{
    public class VotanteMarcarYaVotoRequest
    {
        public string IdVotante { get; set; } = string.Empty;
        public int IdUsuarioMarca { get; set; }
        public string? Observacion { get; set; }
    }
}

