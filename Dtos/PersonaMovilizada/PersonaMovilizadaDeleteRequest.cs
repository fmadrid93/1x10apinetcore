using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dtos.PersonaMovilizada
{
    public class PersonaMovilizadaDeleteRequest
    {
        public int IdPersonaMovilizada { get; set; }
        public int IdUsuarioMovilizador { get; set; }
    }
}