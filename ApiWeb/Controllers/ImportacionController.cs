using System;
using System.Data;
using System.Security.Claims;
using Application.Importacion;
using Application.Reportes;
using Dtos.Importacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiWeb.Controllers
{
    [Authorize(Roles = "ADMINISTRADOR,SUPERADMINISTRADOR,SUPERADMIN")]
    [ApiController]
    [Route("api/[controller]")]
    public class ImportacionController : ControllerBase
    {
        private readonly ImportacionService _service = new ImportacionService();
        private readonly IExcelExportService _excelExportService = new ExcelExportService();

        private int ObtenerIdUsuarioActual()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }

        private int? ObtenerIdTerritorioActual()
        {
            var valor = User.FindFirstValue("idTerritorio");
            return string.IsNullOrEmpty(valor) ? (int?)null : int.Parse(valor);
        }

        [HttpPost("masiva-jerarquica")]
        public IActionResult ImportacionMasivaJerarquica([FromBody] ImportacionMasivaRequest request)
        {
            try
            {
                int idAdmin = ObtenerIdUsuarioActual();
                int? idTerritorio = ObtenerIdTerritorioActual();

                var res = _service.ProcesarImportacionMasiva(request, idAdmin, idTerritorio);
                return Ok(new
                {
                    exito = res.Exito ? 1 : 0,
                    dato = res,
                    status = "ok",
                    mensaje = $"Procesados {res.TotalFilas} filas: {res.GerentesCreados} gerentes, {res.MovilizadoresCreados} movilizadores, {res.VotantesInsertados} votantes, {res.RecintosVinculados} recintos vinculados."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    exito = 0,
                    status = "error",
                    mensaje = $"Error en importación masiva: {ex.Message}"
                });
            }
        }

        [HttpGet("plantilla-excel")]
        public IActionResult DescargarPlantillaExcel()
        {
            try
            {
                var dt = new DataTable("PlantillaImportacion");
                dt.Columns.Add("NombreGerente", typeof(string));
                dt.Columns.Add("CiGerente", typeof(string));
                dt.Columns.Add("CelularGerente", typeof(string));
                dt.Columns.Add("NombreMovilizador", typeof(string));
                dt.Columns.Add("CiMovilizador", typeof(string));
                dt.Columns.Add("CelularMovilizador", typeof(string));
                dt.Columns.Add("VotanteNombres", typeof(string));
                dt.Columns.Add("VotanteApellidos", typeof(string));
                dt.Columns.Add("VotanteCI", typeof(string));
                dt.Columns.Add("VotanteCelular", typeof(string));
                dt.Columns.Add("VotanteFechaNacimiento", typeof(string));
                dt.Columns.Add("NombreRecinto", typeof(string));
                dt.Columns.Add("Zona", typeof(string));

                dt.Rows.Add("Carlos Mendoza", "3456789", "0981111111", "Juan Perez", "4567890", "0982222222", "Maria", "Gonzalez", "5678901", "0981123456", "15/05/1998", "Colegio Nacional Capital", "Centro");
                dt.Rows.Add("Carlos Mendoza", "3456789", "0981111111", "Juan Perez", "4567890", "0982222222", "Pedro", "Benitez", "6789012", "0982234567", "20/11/1985", "Colegio San Jose", "Centro");
                dt.Rows.Add("Carlos Mendoza", "3456789", "0981111111", "Rosa Martinez", "4890123", "0983333333", "Lucas", "Duarte", "7890123", "0983345678", "10/02/2003", "Escuela Republica de Colombia", "San Roque");

                return _excelExportService.ExportarXlsx(
                    dt,
                    "Plantilla Carga",
                    "plantilla_importacion_masiva.xlsx",
                    ("NombreGerente", "NombreGerente"),
                    ("CiGerente", "CiGerente"),
                    ("CelularGerente", "CelularGerente"),
                    ("NombreMovilizador", "NombreMovilizador"),
                    ("CiMovilizador", "CiMovilizador"),
                    ("CelularMovilizador", "CelularMovilizador"),
                    ("VotanteNombres", "VotanteNombres"),
                    ("VotanteApellidos", "VotanteApellidos"),
                    ("VotanteCI", "VotanteCI"),
                    ("VotanteCelular", "VotanteCelular"),
                    ("VotanteFechaNacimiento", "VotanteFechaNacimiento"),
                    ("NombreRecinto", "NombreRecinto"),
                    ("Zona", "Zona")
                );
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    exito = 0,
                    status = "error",
                    mensaje = $"Error al generar plantilla Excel: {ex.Message}"
                });
            }
        }

        [HttpPost("veedores-excel")]
        public IActionResult ImportacionVeedoresExcel([FromBody] ImportacionVeedoresRequest request)
        {
            try
            {
                int idAdmin = ObtenerIdUsuarioActual();
                int? idTerritorio = ObtenerIdTerritorioActual();

                var res = _service.ProcesarImportacionVeedores(request, idAdmin, idTerritorio);
                return Ok(new
                {
                    exito = res.Exito ? 1 : 0,
                    dato = res,
                    status = "ok",
                    mensaje = $"Procesados {res.TotalFilas} filas: {res.VeedoresCreados} veedores creados, {res.VeedoresActualizados} actualizados, {res.RecintosVinculados} recintos vinculados."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    exito = 0,
                    status = "error",
                    mensaje = $"Error en importación de veedores: {ex.Message}"
                });
            }
        }

        [HttpGet("plantilla-veedores-excel")]
        public IActionResult DescargarPlantillaVeedoresExcel()
        {
            try
            {
                var dt = new DataTable("PlantillaVeedores");
                dt.Columns.Add("CI", typeof(string));
                dt.Columns.Add("Nombres", typeof(string));
                dt.Columns.Add("Apellidos", typeof(string));
                dt.Columns.Add("Celular", typeof(string));
                dt.Columns.Add("Email", typeof(string));
                dt.Columns.Add("Usuario", typeof(string));
                dt.Columns.Add("Password", typeof(string));
                dt.Columns.Add("NombreRecinto", typeof(string));
                dt.Columns.Add("Mesa", typeof(string));
                dt.Columns.Add("PermisoMarcacion", typeof(string));

                dt.Rows.Add("1234567", "Juan", "Perez", "0981112233", "juan@gmail.com", "jperez", "123456", "COL. NAC. DE ENSEÑANZA MEDIA D. DR. EUSEBIO AYALA", "1", "VOTO");
                dt.Rows.Add("2345678", "Maria", "Gomez", "0982334455", "maria@gmail.com", "mgomez", "123456", "COL. NAC. DE ENSEÑANZA MEDIA D. DR. EUSEBIO AYALA", "2", "PC");
                dt.Rows.Add("3456789", "Pedro", "Lopez", "0983556677", "pedro@gmail.com", "plopez", "123456", "UNIDAD EDUC. JUAN B. ALBERDI", "1", "AMBOS");

                return _excelExportService.ExportarXlsx(
                    dt,
                    "Plantilla Veedores",
                    "plantilla_importacion_veedores.xlsx",
                    ("CI", "CI"),
                    ("Nombres", "Nombres"),
                    ("Apellidos", "Apellidos"),
                    ("Celular", "Celular"),
                    ("Email", "Email"),
                    ("Usuario", "Usuario"),
                    ("Password", "Password"),
                    ("NombreRecinto", "NombreRecinto"),
                    ("Mesa", "Mesa"),
                    ("PermisoMarcacion", "PermisoMarcacion")
                );
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    exito = 0,
                    status = "error",
                    mensaje = $"Error al generar plantilla Excel de veedores: {ex.Message}"
                });
            }
        }
    }
}
