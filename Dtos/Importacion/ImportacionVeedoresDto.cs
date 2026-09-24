using System;
using System.Collections.Generic;

namespace Dtos.Importacion
{
    public class ImportacionVeedorItemDto
    {
        public string? CI { get; set; }
        public string Nombres { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public string? Celular { get; set; }
        public string? Email { get; set; }
        public string? Usuario { get; set; }
        public string? Password { get; set; }
        public string? NombreRecinto { get; set; }
        public string? IdRecinto { get; set; }
        public string? Mesa { get; set; }
        public string? Rol { get; set; } // "VERIFICADOR_VOTO" (default) o "ENCARGADO_RECINTO"
        public string? PermisoMarcacion { get; set; } // "AMBOS", "SOLO_MESA", "SOLO_PC"
    }

    public class ImportacionVeedoresRequest
    {
        public List<ImportacionVeedorItemDto> Filas { get; set; } = new List<ImportacionVeedorItemDto>();
        public int? IdTerritorioPorDefecto { get; set; }
        public string? IdRecintoPorDefecto { get; set; }
        public string ClavePorDefecto { get; set; } = "123456";
    }

    public class ImportacionVeedoresResultadoDto
    {
        public int TotalFilas { get; set; }
        public int VeedoresCreados { get; set; }
        public int VeedoresActualizados { get; set; }
        public int VeedoresOmitidos { get; set; }
        public int RecintosVinculados { get; set; }
        public List<string> Errores { get; set; } = new List<string>();
        public bool Exito => Errores.Count == 0 || VeedoresCreados > 0 || VeedoresActualizados > 0;
    }
}
