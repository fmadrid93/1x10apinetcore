using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Application.Configuracion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiWeb.Controllers
{
    // GET: cualquier rol autenticado puede leer (lo necesitan Gerente/Movilizador
    // para saber qué campos son obligatorios al llenar el formulario de registro).
    // POST: solo ADMINISTRADOR puede modificar estos parámetros.
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ConfiguracionController : ControllerBase
    {
        private readonly ConfiguracionService _service = new ConfiguracionService();

        // El IdTerritorio confiable sale siempre del JWT (claims), nunca del body/query
        // del request: cada Admin Territorial solo puede leer/tocar la configuración de
        // "campos obligatorios" de SU PROPIA estructura. Un SuperAdmin (sin territorio en
        // el token) administra el default global que heredan los territorios sin config propia.
        private int? ObtenerIdTerritorioActual()
        {
            var valor = User.FindFirstValue("idTerritorio");
            return string.IsNullOrEmpty(valor) ? (int?)null : int.Parse(valor);
        }

        [HttpGet("permitir-duplicados")]
        public IActionResult ObtenerPermitirDuplicados()
        {
            try
            {
                int? idTerritorio = ObtenerIdTerritorioActual();
                bool permitir = _service.ObtenerPermitirDuplicados(idTerritorio);
                return Ok(new
                {
                    exito = 1,
                    dato = new { permitirDuplicados = permitir, idTerritorio, esConfigGlobal = idTerritorio == null },
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
                int? idTerritorio = ObtenerIdTerritorioActual();
                bool guardado = _service.GuardarPermitirDuplicados(idTerritorio, request.PermitirDuplicados);
                return Ok(new
                {
                    exito = guardado ? 1 : 0,
                    dato = new { permitirDuplicados = request.PermitirDuplicados, idTerritorio },
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
                int? idTerritorio = ObtenerIdTerritorioActual();
                var seleccionados = _service.ObtenerCamposObligatorios(idTerritorio);
                var disponibles = CamposVotanteCatalogo.CamposConfigurables.Select(c => new
                {
                    codigo = c.Codigo,
                    etiqueta = c.Etiqueta,
                    obligatorio = seleccionados.Contains(c.Codigo)
                });

                return Ok(new
                {
                    exito = 1,
                    dato = new { campos = disponibles, idTerritorio, esConfigGlobal = idTerritorio == null },
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
                int? idTerritorio = ObtenerIdTerritorioActual();
                bool guardado = _service.GuardarCamposObligatorios(idTerritorio, request.Campos);
                return Ok(new
                {
                    exito = guardado ? 1 : 0,
                    dato = new { campos = request.Campos, idTerritorio },
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
