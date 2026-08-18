using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dtos.Usuario
{
    public class CambiarClaveRequest
    {
        public int IdUsuario { get; set; }
        public string NuevaClave { get; set; } = string.Empty;
        public string? Motivo { get; set; }
    }
}
