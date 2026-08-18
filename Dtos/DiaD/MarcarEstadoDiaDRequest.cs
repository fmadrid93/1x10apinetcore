using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dtos.DiaD
{
    public class MarcarEstadoDiaDRequest
    {
        public int IdPersonaMovilizada { get; set; }
        public int IdUsuarioMarca { get; set; }
        public string EstadoDiaD { get; set; } = string.Empty;
        public string? Observacion { get; set; }
    }
}
