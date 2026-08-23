using System;
using System.Threading.Tasks;
using Application.WhatsApp;
using Dtos.WhatsApp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace ApiWeb.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WhatsAppController : ControllerBase
    {
        private readonly WhatsAppService _service;

        public WhatsAppController(IConfiguration configuration)
        {
            _service = new WhatsAppService(configuration);
        }

        [HttpGet("config")]
        public async Task<IActionResult> ObtenerConfig()
        {
            try
            {
                var cfg = await _service.ObtenerConfiguracionAsync();
                return Ok(new { exito = 1, dato = cfg, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpGet("ip")]
        public async Task<IActionResult> ObtenerIp()
        {
            try
            {
                var ip = await _service.ObtenerIpSalidaAsync();
                return Ok(new { exito = 1, dato = new { ip }, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("sesiones/{idUsuario}")]
        public async Task<IActionResult> ListarSesiones(int idUsuario)
        {
            try
            {
                var sesiones = await _service.ListarSesionesUsuarioAsync(idUsuario);
                return Ok(new { exito = 1, dato = sesiones, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("sesiones")]
        public async Task<IActionResult> CrearSesion([FromBody] WhatsAppCreateSessionRequest request)
        {
            try
            {
                var sesion = await _service.CrearSesionAsync(request.IdUsuario, request.NombreAlias);
                return Ok(new { exito = 1, dato = sesion, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("qr/{sessionId}")]
        public async Task<IActionResult> ObtenerQr(string sessionId)
        {
            try
            {
                var qr = await _service.ObtenerQrAsync(sessionId);
                return Ok(new { exito = 1, dato = qr, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [Authorize]
        [HttpDelete("sesiones/{sessionId}")]
        public async Task<IActionResult> EliminarSesion(string sessionId)
        {
            try
            {
                var ok = await _service.EliminarSesionAsync(sessionId);
                return Ok(new { exito = ok ? 1 : 0, dato = ok, status = ok ? "Sesión reiniciada" : "Error al reiniciar" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("destinatarios-nodo/{idUsuario}")]
        public IActionResult ObtenerDestinatariosNodo(int idUsuario, [FromQuery] string rol)
        {
            try
            {
                var lista = _service.ObtenerDestinatariosNodo(idUsuario, rol);
                return Ok(new { exito = 1, dato = lista, total = lista.Count, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("enviar-difusion")]
        public async Task<IActionResult> EnviarDifusion([FromBody] WhatsAppDifusionRequest request)
        {
            try
            {
                var resultado = await _service.EnviarDifusionNodoAsync(request);
                return Ok(new { exito = 1, dato = resultado, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [Authorize(Roles = "ADMINISTRADOR")]
        [HttpGet("monitoreo-global")]
        public async Task<IActionResult> ObtenerMonitoreoGlobal()
        {
            try
            {
                int idUsuario = 0;
                var claimId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(claimId))
                {
                    int.TryParse(claimId, out idUsuario);
                }

                var lista = await _service.ObtenerMonitoreoGlobalAsync(idUsuario);
                return Ok(new { exito = 1, dato = lista, total = lista.Count, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }
    }
}
