using Application.MovilizadorMeta;
using Application.Reportes;
using Dtos.MovilizadorMeta;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiWeb.Controllers
{
    [Authorize(Roles = "ADMINISTRADOR,GERENTE")]
    [ApiController]
    [Route("api/[controller]")]
    public class MovilizadorMetaController : ControllerBase
    {
        private readonly MovilizadorMetaService _service = new MovilizadorMetaService();
		private readonly Application.Reportes.IExcelExportService _excelExportService = new ExcelExportService();

		[HttpGet("listar")]
        public IActionResult Listar()
        {
            try
            {
                var ds = _service.Listar();
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }
        [HttpPost("guardar")]
        public IActionResult Guardar([FromBody] MovilizadorMetaRequest request)
        {
            try
            {
                var ds = _service.Guardar(request.IdUsuarioMovilizador, request.MetaObjetivo);
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpGet("obtener/{idUsuarioMovilizador}")]
        public IActionResult Obtener(int idUsuarioMovilizador)
        {
            try
            {
                var ds = _service.Obtener(idUsuarioMovilizador);
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }
        [HttpGet("listar-por-gerente/{idGerente}")]
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
		[HttpGet("reporte-excel")]
		public IActionResult ReporteExcel()
		{
			try
			{
				var ds = _service.Listar();

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
					"Metas Movilizador",
					"metas_movilizador.xlsx",
					("IdUsuarioMovilizador", "IdUsuarioMovilizador"),
					("Movilizador", "Movilizador"),
					("MetaObjetivo", "MetaObjetivo")
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

		[HttpGet("listar-por-gerente-excel/{idGerente}")]
		public IActionResult ListarPorGerenteExcel(int idGerente)
		{
			try
			{
				var ds = _service.ListarPorGerente(idGerente);

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
					"Metas Gerente",
					"gerente_metas.xlsx",
					("IdUsuarioMovilizador", "IdUsuarioMovilizador"),
					("Movilizador", "Movilizador"),
					("MetaObjetivo", "MetaObjetivo")
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