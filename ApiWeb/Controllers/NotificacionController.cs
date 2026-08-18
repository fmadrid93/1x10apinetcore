using Application.Notificacion;
using Dtos.Notificacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificacionController : ControllerBase
    {
        private readonly NotificacionService _service = new NotificacionService();

        [HttpPost("registrar-token")]
        public IActionResult RegistrarToken([FromBody] RegistrarTokenRequest request)
        {
            try
            {
                var ds = _service.RegistrarToken(
                    request.IdUsuario,
                    request.Token,
                    request.Plataforma
                );

                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }
    }
}