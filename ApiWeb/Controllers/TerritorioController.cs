using Application.Reportes;
using Application.Territorio;
using Dtos.Territorio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiWeb.Controllers
{
    [Authorize(Roles = "ADMINISTRADOR")]
    [ApiController]
    [Route("api/[controller]")]
    public class TerritorioController : ControllerBase
    {
        private readonly TerritorioService _service = new TerritorioService();
		private readonly Application.Reportes.IExcelExportService _excelExportService = new ExcelExportService(); 

		[HttpPost("insertar")]
        public IActionResult Insertar([FromBody] TerritorioInsertRequest request)
        {
            try
            {
                var ds = _service.Insertar(request.IdTerritorioPadre, request.Nombre, request.TipoTerritorio, request.Codigo);
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }
        [HttpPost("actualizar")]
        public IActionResult Actualizar([FromBody] TerritorioInsertRequest request)
        {
            try
            {
                var ds = _service.Actualizar(request.IdTerritorio,request.IdTerritorioPadre, request.Nombre, request.TipoTerritorio, request.Codigo, request.Activo);
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpGet("listar")]
        public IActionResult Listar([FromQuery] bool soloActivos = true)
        {
            try
            {
                var ds = _service.Listar(soloActivos);
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpGet("obtener/{idTerritorio}")]
        public IActionResult ObtenerPorId(int idTerritorio)
        {
            try
            {
                var ds = _service.ObtenerPorId(idTerritorio);
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }
		[HttpGet("reporte-excel")]
		public IActionResult ReporteExcel([FromQuery] bool soloActivos = true)
		{
			try
			{
				var ds = _service.Listar(soloActivos);

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
					"Territorios",
					"territorios.xlsx",
					("IdTerritorio", "IdTerritorio"),
					("Nombre", "Nombre"),
					("TipoTerritorio", "TipoTerritorio"),
					("NombrePadre", "NombrePadre"),
					("Codigo", "Codigo"),
					("Activo", "Activo")
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
		[HttpGet("reporte-pdf")]
        public IActionResult ReportePdf([FromQuery] bool soloActivos = true)
        {
            try
            {
                var ds = _service.Listar(soloActivos);

                if (ds == null || ds.Rows.Count == 0)
                {
                    return BadRequest(new
                    {
                        exito = 0,
                        dato = (object?)null,
                        status = "No hay datos para exportar"
                    });
                }

                var bytes = Application.Reportes.ReportePdfService.GenerarPdfTerritorios(ds);
                return File(bytes, "application/pdf", "territorios.pdf");
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