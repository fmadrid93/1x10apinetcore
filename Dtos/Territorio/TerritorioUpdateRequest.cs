namespace Dtos.Territorio
{
    public class TerritorioUpdateRequest
    {
        public int IdTerritorio { get; set; }
        public int? IdTerritorioPadre { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string TipoTerritorio { get; set; } = string.Empty;
        public string? Codigo { get; set; }
        public bool Activo { get; set; }
    }
}