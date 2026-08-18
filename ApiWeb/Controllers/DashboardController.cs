using Application.Dashboard;
using Application.Reportes;
using Domain;
using Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiWeb.Controllers
{
   // [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly DashboardService _service = new DashboardService();
        private readonly Application.Reportes.IExcelExportService _excelExportService = new ExcelExportService();

        [HttpGet("admin-kpis")]
        public IActionResult AdminKpis()
        {
            try
            {
                var ds = _service.AdminKpis();
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpGet("admin-ranking-movilizadores")]
        public IActionResult AdminRankingMovilizadores()
        {
            try
            {
                var ds = _service.AdminRankingMovilizadores();
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpGet("admin-ranking-zonas")]
        public IActionResult AdminRankingZonas()
        {
            try
            {
                var ds = _service.AdminRankingZonas();
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpGet("admin-diad-por-zona")]
        public IActionResult AdminDiaDPorZona()
        {
            try
            {
                var ds = _service.AdminDiaDPorZona();
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpGet("gerente-kpis/{idGerente}")]
        public IActionResult GerenteKpis(int idGerente)
        {
            try
            {
                var ds = _service.GerenteKpis(idGerente);
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpGet("gerente-ranking-movilizadores/{idGerente}")]
        public IActionResult GerenteRankingMovilizadores(int idGerente)
        {
            try
            {
                var ds = _service.GerenteRankingMovilizadores(idGerente);
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpGet("gerente-alertas/{idGerente}")]
        public IActionResult GerenteAlertas(int idGerente)
        {
            try
            {
                var ds = _service.GerenteAlertas(idGerente);
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }
        [HttpGet("admin-detalle-zona/{idTerritorio}")]
        public IActionResult AdminDetalleZona(int idTerritorio)
        {
            try
            {
                var ds = _service.AdminDetalleZona(idTerritorio);
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }
        [HttpGet("admin-diad-resumen")]
        [Authorize(Roles = "ADMINISTRADOR")]
        public IActionResult AdminDiaDResumen([FromQuery] int? horaInicio, [FromQuery] int? horaFin)
        {
            try
            {
                var ds = _service.AdminDiaDResumen(horaInicio, horaFin);

                return Ok(new
                {
                    exito = 1,
                    dato = ds
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    exito = 0,
                    status = ex.Message
                });
            }
        }

        [HttpGet("gerente-diad-resumen/{idGerente}")]
        [Authorize(Roles = "GERENTE,ADMINISTRADOR")]
        public IActionResult GerenteDiaDResumen(int idGerente, [FromQuery] int? horaInicio, [FromQuery] int? horaFin)
        {
            try
            {
                var ds = _service.GerenteDiaDResumen(idGerente, horaInicio, horaFin);

                return Ok(new
                {
                    exito = 1,
                    dato = ds
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    exito = 0,
                    status = ex.Message
                });
            }
        }
        [HttpGet("admin-comparativo-zonas")]
        [Authorize(Roles = "ADMINISTRADOR")]
        public IActionResult AdminComparativoZonas()
        {
            try
            {
                var ds = _service.AdminComparativoZonas();

                return Ok(new
                {
                    exito = 1,
                    dato = ds
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    exito = 0,
                    status = ex.Message
                });
            }
        }
        [HttpGet("admin-comparativo-gerentes")]
        [Authorize(Roles = "ADMINISTRADOR")]
        public IActionResult AdminComparativoGerentes()
        {
            try
            {
                var ds = _service.AdminComparativoGerentes();

                return Ok(new
                {
                    exito = 1,
                    dato = ds
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    exito = 0,
                    status = ex.Message
                });
            }
        }
        [HttpGet("admin-ranking-zonas-excel")]
        [Authorize(Roles = "ADMINISTRADOR")]
        public IActionResult AdminRankingZonasExcel()
        {
            try
            {
                var ds = _service.AdminRankingZonas();

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
                    "Ranking Zonas",
                    "ranking_zonas.xlsx",
                    ("IdTerritorio", "IdTerritorio"),
                    ("Territorio", "Territorio"),
                    ("TotalMovilizadores", "TotalMovilizadores"),
                    ("TotalMovilizados", "TotalMovilizados"),
                    ("PorcentajeCumplimiento", "PorcentajeCumplimiento"),
                    ("Semaforo", "Semaforo")
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
        [HttpGet("admin-ranking-movilizadores-excel")]
        [Authorize(Roles = "ADMINISTRADOR")]
        public IActionResult AdminRankingMovilizadoresExcel()
        {
            try
            {
                var ds = _service.AdminRankingMovilizadores();

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
                    "Ranking Movilizadores",
                    "ranking_movilizadores.xlsx",
                    ("IdUsuario", "IdUsuario"),
                    ("Movilizador", "Movilizador"),
                    ("MetaObjetivo", "MetaObjetivo"),
                    ("TotalRegistrados", "TotalRegistrados"),
                    ("TotalYaVoto", "TotalYaVoto"),
                    ("TotalNoContactado", "TotalNoContactado"),
                    ("PorcentajeCumplimiento", "PorcentajeCumplimiento"),
                    ("Semaforo", "Semaforo")
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
        [HttpGet("admin-diad-por-zona-excel")]
        [Authorize(Roles = "ADMINISTRADOR")]
        public IActionResult AdminDiaDPorZonaExcel()
        {
            try
            {
                var ds = _service.AdminDiaDPorZona();

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
                    "Dia D Zonas",
                    "diad_zonas.xlsx",
                    ("IdTerritorio", "IdTerritorio"),
                    ("Territorio", "Territorio"),
                    ("TotalMovilizadores", "TotalMovilizadores"),
                    ("MovilizadoresReportando", "MovilizadoresReportando"),
                    ("PorcentajeMovilizadoresReportando", "PorcentajeMovilizadoresReportando"),
                    ("TotalYaVoto", "TotalYaVoto"),
                    ("TotalNoContactado", "TotalNoContactado")
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
        [HttpGet("gerente-ranking-movilizadores-excel/{idGerente}")]
        [Authorize(Roles = "GERENTE,ADMINISTRADOR")]
        public IActionResult GerenteRankingMovilizadoresExcel(int idGerente)
        {
            try
            {
                var ds = _service.GerenteRankingMovilizadores(idGerente);

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
                    "Gerente Movilizadores",
                    "gerente_movilizadores.xlsx",
                    ("IdUsuario", "IdUsuario"),
                    ("Movilizador", "Movilizador"),
                    ("MetaObjetivo", "MetaObjetivo"),
                    ("TotalRegistrados", "TotalRegistrados"),
                    ("TotalYaVoto", "TotalYaVoto"),
                    ("TotalNoContactado", "TotalNoContactado"),
                    ("PorcentajeCumplimiento", "PorcentajeCumplimiento"),
                    ("Semaforo", "Semaforo")
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
        [HttpGet("gerente-ranking-movilizadores-pdf/{idGerente}")]
        [Authorize(Roles = "GERENTE,ADMINISTRADOR")]
        public IActionResult GerenteRankingMovilizadoresPdf(int idGerente)
        {
            try
            {
                var ds = _service.GerenteRankingMovilizadores(idGerente);

                if (ds == null || ds.Rows.Count == 0)
                {
                    return BadRequest(new { exito = 0, dato = (object?)null, status = "No hay datos para exportar" });
                }

                var bytes = Application.Reportes.ReportePdfService.GenerarPdfMovilizadoresGerente(ds);
                return File(bytes, "application/pdf", "gerente_movilizadores.pdf");
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }
    }

}