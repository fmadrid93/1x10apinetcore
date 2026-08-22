using Application.Recinto;
using Application.Reportes;
using Application.Territorio;
using Dtos.Territorio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiWeb.Controllers
{
   // [Authorize(Roles = "ADMINISTRADOR")]
    [ApiController]
    [Route("api/[controller]")]
    public class RecintoController : ControllerBase
    {
        private readonly RecintoService _service = new RecintoService();
		private readonly Application.Reportes.IExcelExportService _excelExportService = new ExcelExportService(); 

		//[HttpPost("insertar")]
       

        [HttpGet("listar/{idTerritorio}")]
        public IActionResult ListarXTerritorio(int idTerritorio)
        {
            try
            {
                var ds = _service.ListarXTerritorio(idTerritorio);
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }
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

    }
}