using Application.Configuracion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiWeb.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ConfiguracionController : ControllerBase
    {
        private readonly ConfiguracionService _service = new ConfiguracionService();

        [HttpGet("permitir-duplicados")]
        public IActionResult ObtenerPermitirDuplicados()
        {
            try
            {
                bool permitir = _service.ObtenerPermitirDuplicados();
                return Ok(new
                {
                    exito = 1,
                    dato = new { permitirDuplicados = permitir },
                    status = "ok"
                });
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

        public class GuardarPermitirDuplicadosRequest
        {
            public bool PermitirDuplicados { get; set; }
        }

        [HttpPost("permitir-duplicados")]
        public IActionResult GuardarPermitirDuplicados([FromBody] GuardarPermitirDuplicadosRequest request)
        {
            try
            {
                bool guardado = _service.GuardarPermitirDuplicados(request.PermitirDuplicados);
                return Ok(new
                {
                    exito = guardado ? 1 : 0,
                    dato = new { permitirDuplicados = request.PermitirDuplicados },
                    status = guardado ? "ok" : "Error al guardar configuración"
                });
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
