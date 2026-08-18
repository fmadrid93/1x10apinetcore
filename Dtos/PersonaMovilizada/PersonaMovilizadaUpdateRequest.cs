using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dtos.PersonaMovilizada
{
    public class PersonaMovilizadaUpdateRequest
    {
        public int IdPersonaMovilizada { get; set; }
        public int IdUsuarioMovilizador { get; set; }
        public int? IdTerritorio { get; set; }
        public string Nombres { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public string? CI { get; set; }
        public string? Celular { get; set; }
        public string? DireccionReferencia { get; set; }


        public string? Sexo { get; set; }
        public string? RangoEdad { get; set; }
        public string? RecintoVotacion { get; set; }
        public bool? RequiereAyudaVotar { get; set; }
        public string? NivelCompromiso { get; set; }
        public string? Observaciones { get; set; }
        public decimal? Latitud { get; set; }
        public decimal? Longitud { get; set; }
        public string? IdRecinto { get; set; }
    }
}
