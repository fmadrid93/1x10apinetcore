using System;
using System.Security.Claims;
using Application.Importacion;
using Dtos.Importacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiWeb.Controllers
{
    [Authorize(Roles = "ADMINISTRADOR")]
    [ApiController]
    [Route("api/[controller]")]
    public class ImportacionController : ControllerBase
    {
        private readonly ImportacionService _service = new ImportacionService();

        private int ObtenerIdUsuarioActual()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }

        private int? ObtenerIdTerritorioActual()
        {
            var valor = User.FindFirstValue("idTerritorio");
            return string.IsNullOrEmpty(valor) ? (int?)null : int.Parse(valor);
        }

        [HttpPost("masiva-jerarquica")]
        public IActionResult ImportacionMasivaJerarquica([FromBody] ImportacionMasivaRequest request)
        {
            try
            {
                int idAdmin = ObtenerIdUsuarioActual();
                int? idTerritorio = ObtenerIdTerritorioActual();

                var res = _service.ProcesarImportacionMasiva(request, idAdmin, idTerritorio);
                return Ok(new
                {
                    exito = res.Exito ? 1 : 0,
                    dato = res,
                    status = "ok",
                    mensaje = $"Procesados {res.TotalFilas} filas: {res.GerentesCreados} gerentes, {res.MovilizadoresCreados} movilizadores, {res.VotantesInsertados} votantes, {res.RecintosVinculados} recintos vinculados."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    exito = 0,
                    status = "error",
                    mensaje = $"Error en importación masiva: {ex.Message}"
                });
            }
        }
    }
}
