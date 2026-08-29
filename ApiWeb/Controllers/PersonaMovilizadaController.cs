using Application.PersonaMovilizada;
using Application.Reportes;
using Dtos.PersonaMovilizada;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace ApiWeb.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PersonaMovilizadaController : ControllerBase
    {
        private readonly PersonaMovilizadaService _service;
        private readonly Application.Reportes.IExcelExportService _excelExportService = new ExcelExportService();

        public PersonaMovilizadaController(IConfiguration configuration)
        {
            _service = new PersonaMovilizadaService(configuration);
        }

        [HttpPost("insertar")]
        public IActionResult Insertar([FromBody] PersonaMovilizadaInsertRequest request)
        {
            try
            {
                var ds = _service.Insertar(
                   request.IdUsuarioMovilizador,
                   request.IdTerritorio,
                   request.Nombres,
                   request.Apellidos,
                   request.CI,
                   request.Celular,
                   request.DireccionReferencia,
                   request.Sexo,
                   request.RangoEdad,
                   request.RecintoVotacion,
                   request.IdRecinto,
                   request.RequiereAyudaVotar,
                   request.NivelCompromiso,
                   request.Observaciones,
                   request.Latitud,
                   request.Longitud
               );

                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpPost("actualizar")]
        public IActionResult Actualizar([FromBody] PersonaMovilizadaUpdateRequest request)
        {
            try
            {
                var ds = _service.Actualizar(
                  request.IdPersonaMovilizada,
                  request.IdUsuarioMovilizador,
                  request.IdTerritorio,
                  request.Nombres,
                  request.Apellidos,
                  request.CI,
                  request.Celular,
                  request.DireccionReferencia,
                  request.Sexo,
                  request.RangoEdad,
                  request.RecintoVotacion,
                  request.IdRecinto,
                  request.RequiereAyudaVotar,
                  request.NivelCompromiso,
                  request.Observaciones,
                  request.Latitud,
                  request.Longitud
              );

                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpPost("eliminar-logico")]
        public IActionResult EliminarLogico([FromBody] PersonaMovilizadaDeleteRequest request)
        {
            try
            {
                var ds = _service.EliminarLogico(request.IdPersonaMovilizada, request.IdUsuarioMovilizador);
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpGet("obtener/{idPersonaMovilizada}")]
        public IActionResult ObtenerPorId(int idPersonaMovilizada)
        {
            try
            {
                var ds = _service.ObtenerPorId(idPersonaMovilizada);
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpGet("listar-por-movilizador")]
        public IActionResult ListarPorMovilizador([FromQuery] int idUsuarioMovilizador, [FromQuery] string? texto, [FromQuery] string? estadoDiaD)
        {
            try
            {
                var ds = _service.ListarPorMovilizador(idUsuarioMovilizador, texto, estadoDiaD);
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpGet("buscar-general")]
        public IActionResult BuscarGeneral([FromQuery] int? idTerritorio, [FromQuery] int? idUsuarioMovilizador, [FromQuery] string? texto, [FromQuery] string? estadoDiaD)
        {
            try
            {
                var ds = _service.BuscarGeneral(idTerritorio, idUsuarioMovilizador, texto, estadoDiaD);
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpGet("celulares-repetidos")]
        public IActionResult CelularesRepetidos([FromQuery] int? idTerritorio, [FromQuery] int? idUsuarioMovilizador)
        {
            try
            {
                var ds = _service.CelularesRepetidos(idTerritorio, idUsuarioMovilizador);
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpGet("resumen-movilizador/{idUsuarioMovilizador}")]
        public IActionResult ResumenMovilizador(int idUsuarioMovilizador)
        {
            try
            {
                var ds = _service.ResumenMovilizador(idUsuarioMovilizador);
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }
        [HttpGet("reporte-excel/{idUsuarioMovilizador}")]
        public IActionResult ReporteExcel(int idUsuarioMovilizador, [FromQuery] string? texto, [FromQuery] string? estadoDiaD)
        {
            try
            {
                var ds = _service.ListarPorMovilizador(idUsuarioMovilizador, texto, estadoDiaD);

                if (ds == null || ds.Rows.Count == 0)
                {
                    return BadRequest(new
                    {
                        exito = 0,
                        dato = (object?)null,
                        status = "No hay datos para exportar"
                    });
                }

                return _excelExportService.ExportarXlsx(
                    ds,
                    "Mis Personas",
                    "mis_personas.xlsx",
                    ("IdPersonaMovilizada", "IdPersonaMovilizada"),
                    ("Nombres", "Nombres"),
                    ("Apellidos", "Apellidos"),
                    ("CI", "CI"),
                    ("Celular", "Celular"),
                    ("EstadoDiaD", "EstadoDiaD"),
                    ("ObservacionDiaD", "ObservacionDiaD")
                );
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    exito = 0,
                    dato = (object?)null,
                    status = ex.Message
                });
            }
        }
        [HttpGet("reporte-pdf/{idUsuarioMovilizador}")]
        public IActionResult ReportePdf(int idUsuarioMovilizador, [FromQuery] string? texto, [FromQuery] string? estadoDiaD)
        {
            try
            {
                var ds = _service.ListarPorMovilizador(idUsuarioMovilizador, texto, estadoDiaD);

                if (ds == null || ds.Rows.Count == 0)
                {
                    return BadRequest(new
                    {
                        exito = 0,
                        dato = (object?)null,
                        status = "No hay datos para exportar"
                    });
                }

                var table = ds;
                var bytes = Application.Reportes.ReportePdfService.GenerarPdfPersonasMovilizador(table);

                return File(bytes, "application/pdf", "mis_personas.pdf");
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    exito = 0,
                    dato = (object?)null,
                    status = ex.Message
                });
            }
        }
    }
}