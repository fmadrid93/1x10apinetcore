using Application.Mensaje;
using Dtos.Mensaje;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiWeb.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class MensajeController : ControllerBase
    {
        private readonly MensajeService _service = new MensajeService();

        [HttpPost("enviar")]
        public IActionResult Enviar([FromBody] MensajeEnviarRequest request)
        {
            try
            {
                var ds = _service.Enviar(request.IdUsuarioEmisor, request.IdUsuarioDestino, request.Titulo, request.Contenido);
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpGet("bandeja/{idUsuarioDestino}")]
        public IActionResult ListarBandeja(int idUsuarioDestino)
        {
            try
            {
                var ds = _service.ListarBandeja(idUsuarioDestino);
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpPost("marcar-leido/{idMensaje}")]
        public IActionResult MarcarLeido(int idMensaje)
        {
            try
            {
                var ds = _service.MarcarLeido(idMensaje);
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }






            [HttpGet("recibidos/{idUsuarioDestino}")]
            public IActionResult Recibidos(int idUsuarioDestino)
            {
                try
                {
                    var ds = _service.ListarRecibidos(idUsuarioDestino);
                    return Ok(new { exito = 1, dato = ds, status = "ok" });
                }
                catch (Exception ex)
                {
                    return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
                }
            }

            [HttpGet("enviados/{idUsuarioEmisor}")]
            public IActionResult Enviados(int idUsuarioEmisor)
            {
                try
                {
                    var ds = _service.ListarEnviados(idUsuarioEmisor);
                    return Ok(new { exito = 1, dato = ds, status = "ok" });
                }
                catch (Exception ex)
                {
                    return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
                }
            }

           
        }
    }





