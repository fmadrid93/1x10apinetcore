using System.Data;
using System.Security.Claims;
using Application.Reportes;
using Application.Usuario;
using ClosedXML.Excel;
using Dtos.Usuario;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiWeb.Controllers
{
    [Authorize(Roles = "ADMINISTRADOR,GERENTE")]
    [ApiController]
    [Route("api/[controller]")]
    public class UsuarioController : ControllerBase
    {
        private readonly UsuarioService _service = new UsuarioService();
        private readonly Application.Reportes.IExcelExportService _excelExportService = new ExcelExportService();

        // El IdUsuario/IdTerritorio/Rol confiables salen siempre del JWT (claims),
        // nunca del body o la query del request. Ver Manual_Estandares_CSharp_SQLServer_v8.md §40.
        private int ObtenerIdUsuarioActual()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
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

        [HttpPost("insertar")]
        public IActionResult Insertar([FromBody] UsuarioInsertRequest request)
        {
            try
            {
                var ds = _service.Insertar(
                    request.IdRol,
                    request.IdTerritorio,
                    request.IdUsuarioSupervisor,
                    request.Usuario,
                    request.Clave,
                    request.NombreCompleto,
                    request.CI,
                    request.Celular,
                    request.Email,
                    ObtenerIdUsuarioActual(),
                    ObtenerIdTerritorioActual(),
                    ObtenerRolActual(),
                    request.IdRecinto
                );

                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (AccesoDenegadoException ex)
            {
                return StatusCode(403, new { exito = 0, dato = (object?)null, status = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpPost("actualizar")]
        public IActionResult Actualizar([FromBody] UsuarioUpdateRequest request)
        {
            try
            {
                var ds = _service.Actualizar(
                    request.IdUsuario,
                    request.IdRol,
                    request.IdTerritorio,
                    request.IdUsuarioSupervisor,
                    request.NombreCompleto,
                    request.CI,
                    request.Celular,
                    request.Email,
                    request.Activo,
                    ObtenerIdUsuarioActual(),
                    ObtenerIdTerritorioActual(),
                    ObtenerRolActual(),
                    request.Motivo,
                    request.IdRecinto
                );

                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (AccesoDenegadoException ex)
            {
                return StatusCode(403, new { exito = 0, dato = (object?)null, status = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpPost("cambiar-clave")]
        public IActionResult CambiarClave([FromBody] CambiarClaveRequest request)
        {
            try
            {
                var ds = _service.CambiarClave(request.IdUsuario, request.NuevaClave, ObtenerIdUsuarioActual(), ObtenerIdTerritorioActual(), ObtenerRolActual(), request.Motivo);
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (AccesoDenegadoException ex)
            {
                return StatusCode(403, new { exito = 0, dato = (object?)null, status = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpPost("eliminar-logico/{idUsuario}")]
        public IActionResult EliminarLogico(int idUsuario, [FromQuery] string? motivo = null)
        {
            try
            {
                var ds = _service.EliminarLogico(idUsuario, ObtenerIdUsuarioActual(), ObtenerIdTerritorioActual(), ObtenerRolActual(), motivo);
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (AccesoDenegadoException ex)
            {
                return StatusCode(403, new { exito = 0, dato = (object?)null, status = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpGet("listar")]
        public IActionResult Listar([FromQuery] int? idRol, [FromQuery] int? idTerritorio, [FromQuery] int? idUsuarioSupervisor, [FromQuery] bool soloActivos = true)
        {
            try
            {
                var ds = _service.Listar(idRol, idTerritorio, idUsuarioSupervisor, soloActivos, ObtenerIdUsuarioActual(), ObtenerIdTerritorioActual(), ObtenerRolActual());
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpGet("obtener/{idUsuario}")]
        public IActionResult ObtenerPorId(int idUsuario)
        {
            try
            {
                var ds = _service.ObtenerPorId(idUsuario, ObtenerIdUsuarioActual(), ObtenerIdTerritorioActual(), ObtenerRolActual());
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (AccesoDenegadoException ex)
            {
                return StatusCode(403, new { exito = 0, dato = (object?)null, status = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }
        [HttpGet("reporte-excel")]
        public IActionResult ReporteExcel(
      [FromQuery] int? idRol,
      [FromQuery] int? idTerritorio,
      [FromQuery] int? idUsuarioSupervisor,
      [FromQuery] bool soloActivos = true)
        {
            try
            {
                var ds = _service.Listar(idRol, idTerritorio, idUsuarioSupervisor, soloActivos, ObtenerIdUsuarioActual(), ObtenerIdTerritorioActual(), ObtenerRolActual());

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
                    "Usuarios",
                    "usuarios.xlsx",
                    ("IdUsuario", "IdUsuario"),
                    ("Usuario", "Usuario"),
                    ("NombreCompleto", "NombreCompleto"),
                    ("Rol", "Rol"),
                    ("Territorio", "Territorio"),
                    ("Supervisor", "Supervisor"),
                    ("CI", "CI"),
                    ("Celular", "Celular"),
                    ("Email", "Email"),
                    ("Activo", "Activo"), ("TotalPersonas", "TotalPersonas")
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
        [HttpGet("reporte-pdf")]
        public IActionResult ReportePdf([FromQuery] int? idRol, [FromQuery] int? idTerritorio, [FromQuery] int? idUsuarioSupervisor, [FromQuery] bool soloActivos = true)
        {
            try
            {
                var ds = _service.Listar(idRol, idTerritorio, idUsuarioSupervisor, soloActivos, ObtenerIdUsuarioActual(), ObtenerIdTerritorioActual(), ObtenerRolActual());

                if (ds == null || ds.Rows.Count == 0)
                {
                    return BadRequest(new
                    {
                        exito = 0,
                        dato = (object?)null,
                        status = "No hay datos para exportar"
                    });
                }

                var bytes = Application.Reportes.ReportePdfService.GenerarPdfUsuarios(ds);
                return File(bytes, "application/pdf", "usuarios.pdf");
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
