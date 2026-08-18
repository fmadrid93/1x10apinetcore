using System.Data;
using Application.Reportes;
using Application.Usuario;
using ClosedXML.Excel;
using Dtos.Usuario;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiWeb.Controllers
{
  //  [Authorize(Roles = "ADMINISTRADOR")]
    [ApiController]
    [Route("api/[controller]")]
    public class UsuarioController : ControllerBase
    {
        private readonly UsuarioService _service = new UsuarioService();
        private readonly Application.Reportes.IExcelExportService _excelExportService = new ExcelExportService();

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
                    request.Email
                );

                return Ok(new { exito = 1, dato = ds, status = "ok" });
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
                    request.Activo
                );

                return Ok(new { exito = 1, dato = ds, status = "ok" });
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
                var ds = _service.CambiarClave(request.IdUsuario, request.NuevaClave);
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpPost("eliminar-logico/{idUsuario}")]
        public IActionResult EliminarLogico(int idUsuario)
        {
            try
            {
                var ds = _service.EliminarLogico(idUsuario);
                return Ok(new { exito = 1, dato = ds, status = "ok" });
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
                var ds = _service.Listar(idRol, idTerritorio, idUsuarioSupervisor, soloActivos);
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
                var ds = _service.ObtenerPorId(idUsuario);
                return Ok(new { exito = 1, dato = ds, status = "ok" });
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
                var ds = _service.Listar(idRol, idTerritorio, idUsuarioSupervisor, soloActivos);

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
                    ("Activo", "Activo")
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
                var ds = _service.Listar(idRol, idTerritorio, idUsuarioSupervisor, soloActivos);

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