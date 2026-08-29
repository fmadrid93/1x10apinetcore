using System;
using System.Security.Claims;
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

        // Mismo criterio que ConfiguracionController: el IdTerritorio del JWT
        // (null = SuperAdmin) define qué config puede tocar cada quien por
        // defecto. Para el bot/candidato, el SuperAdmin puede además apuntar
        // explícitamente a OTRO municipio (para eso existe la pantalla de
        // "elegir municipio"); un Admin territorial solo puede tocar el suyo.
        private int? ObtenerIdTerritorioActual()
        {
            var valor = User.FindFirstValue("idTerritorio");
            return string.IsNullOrEmpty(valor) ? (int?)null : int.Parse(valor);
        }

        private bool EsSuperAdmin() => ObtenerIdTerritorioActual() == null;

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

        /// <summary>
        /// Trae la config del bot. Un Admin territorial ve siempre la SUYA
        /// (ignora idTerritorio del query aunque lo mande). El SuperAdmin puede
        /// pedir la de un municipio puntual con ?idTerritorio=, o la global si
        /// lo omite.
        /// </summary>
        [Authorize]
        [HttpGet("bot/configuracion")]
        public IActionResult ObtenerBotConfiguracion([FromQuery] int? idTerritorio = null, [FromQuery] string? candidato = null)
        {
            try
            {
                int? propio = ObtenerIdTerritorioActual();
                int? objetivo = EsSuperAdmin() ? idTerritorio : propio;
                var bot = _service.ObtenerBotConfiguracion(objetivo, candidato);
                return Ok(new { exito = 1, dato = bot, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        /// <summary>
        /// Guarda la config del bot. Un Admin territorial solo puede guardar
        /// la SUYA (se le fuerza su propio IdTerritorio del JWT, ignorando lo
        /// que mande en el body). Solo el SuperAdmin puede apuntar a otro
        /// municipio explícito o a la config global.
        /// </summary>
        [Authorize(Roles = "ADMINISTRADOR")]
        [HttpPost("bot/configuracion")]
        public IActionResult GuardarBotConfiguracion([FromBody] WhatsAppBotConfigDto request)
        {
            try
            {
                int? propio = ObtenerIdTerritorioActual();
                if (!EsSuperAdmin())
                {
                    request.IdTerritorio = propio;
                }
                var bot = _service.GuardarBotConfiguracion(request);
                return Ok(new { exito = 1, dato = bot, status = "Configuración del bot guardada exitosamente" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        /// <summary>
        /// Municipios con al menos un Administrador enlazado — para que el
        /// SuperAdmin, desde el móvil, elija a cuál configurarle su
        /// candidato/mensajes sin ver municipios vacíos.
        /// </summary>
        [Authorize(Roles = "ADMINISTRADOR")]
        [HttpGet("bot/municipios")]
        public IActionResult ObtenerMunicipiosConAdministrador()
        {
            if (!EsSuperAdmin())
            {
                return Forbid();
            }
            try
            {
                var lista = _service.ObtenerMunicipiosConAdministrador();
                return Ok(new { exito = 1, dato = lista, total = lista.Count, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpPost("bot/procesar-respuesta")]
        public IActionResult ProcesarRespuestaBot([FromBody] WhatsAppBotRespuestaRequest request)
        {
            try
            {
                var resultado = _service.ProcesarRespuestaBot(request);
                return Ok(new { exito = 1, dato = resultado, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("marcar-consultados")]
        public IActionResult MarcarComoConsultados([FromBody] WhatsAppMarcarConsultadosRequest request)
        {
            try
            {
                if (request?.Celulares != null && request.Celulares.Count > 0)
                {
                    _service.MarcarConsultados(request.Celulares);
                }
                return Ok(new { exito = 1, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, status = ex.Message });
            }
        }

    }
}

