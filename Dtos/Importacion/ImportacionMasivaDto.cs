using System;
using System.Collections.Generic;

namespace Dtos.Importacion
{
    public class ImportacionMasivaItemDto
    {
        public string? NombreGerente { get; set; }
        public string? UsuarioGerente { get; set; }
        public string? CiGerente { get; set; }
        public string? CelularGerente { get; set; }

        public string? NombreMovilizador { get; set; }
        public string? UsuarioMovilizador { get; set; }
        public string? CiMovilizador { get; set; }
        public string? CelularMovilizador { get; set; }

        public int? IdTerritorio { get; set; }
        public string? NombreTerritorio { get; set; }

        public string VotanteNombres { get; set; } = string.Empty;
        public string VotanteApellidos { get; set; } = string.Empty;
        public string? VotanteCI { get; set; }
        public string? VotanteCelular { get; set; }
        public string? VotanteDireccion { get; set; }
        public string? VotanteSexo { get; set; }
        public string? VotanteFechaNacimiento { get; set; }
        public string? VotanteRangoEdad { get; set; }
        public string? VotanteNivelCompromiso { get; set; }

        public string? NombreRecinto { get; set; }
        public string? IdRecinto { get; set; }
    }

    public class ImportacionMasivaRequest
    {
        public List<ImportacionMasivaItemDto> Filas { get; set; } = new List<ImportacionMasivaItemDto>();
        public int? IdTerritorioPorDefecto { get; set; }
        public string ClavePorDefecto { get; set; } = "123456";
    }

    public class ImportacionMasivaResultadoDto
    {
        public int TotalFilas { get; set; }
        public int GerentesCreados { get; set; }
        public int MovilizadoresCreados { get; set; }
        public int VotantesInsertados { get; set; }
        public int VotantesDuplicadosOmitidos { get; set; }
        public int RecintosVinculados { get; set; }
        public List<string> Errores { get; set; } = new List<string>();
        public bool Exito => Errores.Count == 0 || VotantesInsertados > 0;
    }
}
