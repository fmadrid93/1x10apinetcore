using System;
using System.Security.Claims;
using Application.MovilizadorMeta;
using Application.Reportes;
using Dtos.MovilizadorMeta;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiWeb.Controllers
{
    [Authorize(Roles = "ADMINISTRADOR,GERENTE")]
    [ApiController]
    [Route("api/[controller]")]
    public class MovilizadorMetaController : ControllerBase
    {
        private readonly MovilizadorMetaService _service = new MovilizadorMetaService();
        private readonly Application.Reportes.IExcelExportService _excelExportService = new ExcelExportService();

        private int ObtenerIdUsuarioActual()
        {
            var val = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(val, out int id) ? id : 0;
        }

        private int? ObtenerIdTerritorioActual()
        {
            var valor = User.FindFirstValue("idTerritorio");
            return string.IsNullOrEmpty(valor) ? (int?)null : int.Parse(valor);
        }

        private string ObtenerRolActual()
        {
            return User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        }

        [HttpGet("listar")]
        public IActionResult Listar()
        {
            try
            {
                int idUsuario = ObtenerIdUsuarioActual();
                int? idTerritorio = ObtenerIdTerritorioActual();
                string rol = ObtenerRolActual();

                var ds = _service.Listar(idUsuario, idTerritorio, rol);
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpPost("guardar")]
        public IActionResult Guardar([FromBody] MovilizadorMetaRequest request)
        {
            try
            {
                var ds = _service.Guardar(request.IdUsuarioMovilizador, request.MetaObjetivo);
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpGet("obtener/{idUsuarioMovilizador}")]
        public IActionResult Obtener(int idUsuarioMovilizador)
        {
            try
            {
                var ds = _service.Obtener(idUsuarioMovilizador);
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpGet("listar-por-gerente/{idGerente}")]
        public IActionResult ListarPorGerente(int idGerente)
        {
            try
            {
                int idUsuario = ObtenerIdUsuarioActual();
                int? idTerritorio = ObtenerIdTerritorioActual();
                string rol = ObtenerRolActual();

                // Si quien llama es gerente, solo ve sus propios movilizadores
                int targetGerente = rol == "GERENTE" ? idUsuario : idGerente;
                var ds = _service.ListarPorGerente(targetGerente);
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpGet("reporte-excel")]
        public IActionResult ReporteExcel()
        {
            try
            {
                int idUsuario = ObtenerIdUsuarioActual();
                int? idTerritorio = ObtenerIdTerritorioActual();
                string rol = ObtenerRolActual();

                var ds = _service.Listar(idUsuario, idTerritorio, rol);

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
                    "Metas Movilizador",
                    "metas_movilizador.xlsx",
                    ("IdUsuarioMovilizador", "IdUsuarioMovilizador"),
                    ("Movilizador", "Movilizador"),
                    ("MetaObjetivo", "MetaObjetivo")
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

        [HttpGet("listar-por-gerente-excel/{idGerente}")]
        public IActionResult ListarPorGerenteExcel(int idGerente)
        {
            try
            {
                int idUsuario = ObtenerIdUsuarioActual();
                string rol = ObtenerRolActual();

                int targetGerente = rol == "GERENTE" ? idUsuario : idGerente;
                var ds = _service.ListarPorGerente(targetGerente);

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
                    "Metas Gerente",
                    "gerente_metas.xlsx",
                    ("IdUsuarioMovilizador", "IdUsuarioMovilizador"),
                    ("Movilizador", "Movilizador"),
                    ("MetaObjetivo", "MetaObjetivo")
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
    }
}