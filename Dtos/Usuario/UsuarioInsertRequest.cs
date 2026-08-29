using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dtos.Usuario
{
    public class UsuarioInsertRequest
    {
        public int IdRol { get; set; }
        public int? IdTerritorio { get; set; }
        public int? IdUsuarioSupervisor { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public string Clave { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string? CI { get; set; }
        public string? Celular { get; set; }
        public string? Email { get; set; }
        public string? IdRecinto { get; set; }
    }
}