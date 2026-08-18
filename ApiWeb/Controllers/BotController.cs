using Application.Bot;
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
    public class BotController : ControllerBase
    {
        private readonly BotService _service = new BotService(); 


        [HttpGet("ObtenerRecinto/{celular}")]
        public IActionResult ObtenerRecinto(string celular)
        {
            try
            {
                var ds = _service.ObtenerRecinto(celular);
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

    }
}