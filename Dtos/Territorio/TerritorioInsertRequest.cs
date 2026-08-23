using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dtos.Territorio
{
    public class TerritorioInsertRequest
    {
        public int? IdTerritorioPadre { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string TipoTerritorio { get; set; } = string.Empty;
        public string? Codigo { get; set; }
        public int IdTerritorio { get; set; }
        public bool Activo { get; set; }
        public string? UrlServidorWhatsApp { get; set; }
    }
}
