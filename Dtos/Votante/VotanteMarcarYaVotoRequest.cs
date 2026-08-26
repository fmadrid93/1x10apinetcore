using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dtos.Votante
{
    public class VotanteMarcarYaVotoRequest
    {
        public string Observacion { get; set; }
        public string IdVotante { get; set; }
        public int IdUsuarioMarca { get; set; }
    }
}
