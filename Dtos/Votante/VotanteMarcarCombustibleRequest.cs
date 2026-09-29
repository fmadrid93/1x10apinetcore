namespace Dtos.Votante
{
    public class VotanteMarcarCombustibleRequest
    {
        public string IdVotante { get; set; } = string.Empty;
        public int IdUsuarioMarca { get; set; }
        public bool RecibioCombustible { get; set; } = true;
    }
}
