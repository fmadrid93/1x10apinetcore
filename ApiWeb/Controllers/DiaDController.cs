using Application.DiaD;
using Application.Reportes;
using Dtos.DiaD;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiWeb.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DiaDController : ControllerBase
    {
        private readonly DiaDService _service = new DiaDService();
		private readonly Application.Reportes.IExcelExportService _excelExportService = new ExcelExportService();

		[HttpPost("marcar-estado")]
        public IActionResult MarcarEstado([FromBody] MarcarEstadoDiaDRequest request)
        {
            try
            {
                var ds = _service.MarcarEstado(request.IdPersonaMovilizada, request.IdUsuarioMarca, request.EstadoDiaD, request.Observacion);
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpGet("historial-por-persona/{idPersonaMovilizada}")]
        public IActionResult HistorialPorPersona(int idPersonaMovilizada)
        {
            try
            {
                var ds = _service.HistorialPorPersona(idPersonaMovilizada);
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpGet("listar-por-movilizador/{idUsuarioMovilizador}")]
        public IActionResult ListarPorMovilizador(int idUsuarioMovilizador)
        {
            try
            {
                var ds = _service.ListarPorMovilizador(idUsuarioMovilizador);
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpGet("resumen-admin")]
        public IActionResult ResumenAdmin()
        {
            try
            {
                var ds = _service.ResumenAdmin();
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpGet("resumen-gerente/{idGerente}")]
        public IActionResult ResumenGerente(int idGerente)
        {
            try
            {
                var ds = _service.ResumenGerente(idGerente);
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpGet("velocidad-votacion-30min")]
        public IActionResult VelocidadVotacion30Min()
        {
            try
            {
                var ds = _service.VelocidadVotacion30Min();
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpGet("curva-avance")]
        public IActionResult CurvaAvance()
        {
            try
            {
                var ds = _service.CurvaAvance();
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpGet("brecha-meta-hora")]
        public IActionResult BrechaMetaHora()
        {
            try
            {
                var ds = _service.BrechaMetaHora();
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }
		[HttpGet("reporte-excel/{idUsuarioMovilizador}")]
		public IActionResult ReporteExcel(int idUsuarioMovilizador)
		{
			try
			{
				var ds = _service.ListarPorMovilizador(idUsuarioMovilizador);

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
					"Mi Dia D",
					"mi_diad.xlsx",
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

	}
}