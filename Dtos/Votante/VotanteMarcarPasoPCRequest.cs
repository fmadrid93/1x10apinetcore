namespace Dtos.Votante
{
    public class VotanteMarcarPasoPCRequest
    {
        public string IdVotante { get; set; } = string.Empty;
        public int IdUsuarioMarca { get; set; }
    }
}
