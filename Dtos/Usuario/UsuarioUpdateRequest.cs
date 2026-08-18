using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dtos.Usuario
{
    public class UsuarioUpdateRequest
    {
        public int IdUsuario { get; set; }
        public int IdRol { get; set; }
        public int? IdTerritorio { get; set; }
        public int? IdUsuarioSupervisor { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string? CI { get; set; }
        public string? Celular { get; set; }
        public string? Email { get; set; }
        public bool Activo { get; set; } = true;
    }
}
