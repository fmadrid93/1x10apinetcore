using Application.Alerta;
using Application.Reportes;
using Dtos.Alerta;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiWeb.Controllers
{
    [Authorize(Roles = "ADMINISTRADOR,GERENTE")]
    [ApiController]
    [Route("api/[controller]")]
    public class AlertaController : ControllerBase
    {
        private readonly AlertaService _service = new AlertaService();
		private readonly Application.Reportes.IExcelExportService _excelExportService = new ExcelExportService();

		[HttpPost("generar-meta-baja")]
        public IActionResult GenerarMetaBaja()
        {
            try
            {
                var ds = _service.GenerarMetaBaja();
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpPost("generar-sin-reporte-diad")]
        public IActionResult GenerarSinReporteDiaD()
        {
            try
            {
                var ds = _service.GenerarSinReporteDiaD();
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpGet("listar-por-usuario/{idUsuario}")]
        public IActionResult ListarPorUsuario(int idUsuario)
        {
            try
            {
                var ds = _service.ListarPorUsuario(idUsuario);
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpPost("atender")]
        public IActionResult Atender([FromBody] AtenderAlertaRequest request)
        {
            try
            {
                var ds = _service.Atender(request.IdAlerta);
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }
        [HttpPost("generar-sin-carga")]
        [Authorize(Roles = "ADMINISTRADOR")]
        public IActionResult GenerarSinCarga()
        {
            try
            {
                var ds = _service.GenerarSinCarga();
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpPost("generar-bajo-meta")]
        [Authorize(Roles = "ADMINISTRADOR")]
        public IActionResult GenerarBajoMeta()
        {
            try
            {
                var ds = _service.GenerarBajoMeta();
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpGet("listar-por-gerente/{idGerente}")]
        [Authorize(Roles = "GERENTE,ADMINISTRADOR")]
        public IActionResult ListarPorGerente(int idGerente)
        {
            try
            {
                var ds = _service.ListarPorGerente(idGerente);
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }
        [HttpPost("generar-avance-lento")]
        [Authorize(Roles = "ADMINISTRADOR")]
        public IActionResult GenerarAvanceLento()
        {
            try
            {
                var ds = _service.GenerarAvanceLento();
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }
		[HttpGet("listar-por-usuario-excel/{idUsuario}")]
		public IActionResult ListarPorUsuarioExcel(int idUsuario)
		{
			try
			{
				var ds = _service.ListarPorUsuario(idUsuario);

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
					"Alertas",
					"alertas.xlsx",
					("IdAlerta", "IdAlerta"),
					("IdUsuario", "IdUsuario"),
					("Movilizador", "Movilizador"),
					("TipoAlerta", "TipoAlerta"),
					("Descripcion", "Descripcion"),
					("Nivel", "Nivel"),
					("Estado", "Estado"),
					("FechaGeneracion", "FechaGeneracion")
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
		[HttpGet("listar-por-usuario-pdf/{idUsuario}")]
        public IActionResult ListarPorUsuarioPdf(int idUsuario)
        {
            try
            {
                var ds = _service.ListarPorUsuario(idUsuario);

                if (ds == null || ds.Rows.Count == 0)
                {
                    return BadRequest(new { exito = 0, dato = (object?)null, status = "No hay datos para exportar" });
                }

                var bytes = Application.Reportes.ReportePdfService.GenerarPdfAlertas(ds, "Reporte de Alertas");
                return File(bytes, "application/pdf", "alertas.pdf");
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }
    }

}