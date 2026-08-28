using System.Collections.Generic;
using System.Linq;
using Application.Configuracion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiWeb.Controllers
{
    // GET: cualquier rol autenticado puede leer (lo necesitan Gerente/Movilizador
    // para saber qué campos son obligatorios al llenar el formulario de registro).
    // POST: solo ADMINISTRADOR puede modificar estos parámetros globales.
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

        [Authorize(Roles = "ADMINISTRADOR")]
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

        [HttpGet("campos-obligatorios")]
        public IActionResult ObtenerCamposObligatorios()
        {
            try
            {
                var seleccionados = _service.ObtenerCamposObligatorios();
                var disponibles = CamposVotanteCatalogo.CamposConfigurables.Select(c => new
                {
                    codigo = c.Codigo,
                    etiqueta = c.Etiqueta,
                    obligatorio = seleccionados.Contains(c.Codigo)
                });

                return Ok(new
                {
                    exito = 1,
                    dato = new { campos = disponibles },
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

        public class GuardarCamposObligatoriosRequest
        {
            public List<string> Campos { get; set; } = new List<string>();
        }

        [Authorize(Roles = "ADMINISTRADOR")]
        [HttpPost("campos-obligatorios")]
        public IActionResult GuardarCamposObligatorios([FromBody] GuardarCamposObligatoriosRequest request)
        {
            try
            {
                bool guardado = _service.GuardarCamposObligatorios(request.Campos);
                return Ok(new
                {
                    exito = guardado ? 1 : 0,
                    dato = new { campos = request.Campos },
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
