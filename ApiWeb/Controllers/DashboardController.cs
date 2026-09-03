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
        public IActionResult AdminKpis(string idUsuario)
        {
            try
            {
                var ds = _service.AdminKpis( idUsuario);
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpGet("superadmin-resumen-municipios")]
        public IActionResult SuperAdminResumenMunicipios()
        {
            try
            {
                var dt = _service.SuperAdminResumenMunicipios();
                var lista = new List<object>();

                foreach (System.Data.DataRow row in dt.Rows)
                {
                    var celRaw = row["Celular"] != DBNull.Value ? row["Celular"].ToString() : "";
                    lista.Add(new
                    {
                        administrador = row["Administrador"] != DBNull.Value ? row["Administrador"].ToString() : "",
                        celular = FormatearCelularParaguay(celRaw),
                        municipio = row["Municipio"] != DBNull.Value ? row["Municipio"].ToString() : "",
                        concejales = row["Concejales"] != DBNull.Value ? Convert.ToInt32(row["Concejales"]) : 0,
                        punteros = row["Punteros"] != DBNull.Value ? Convert.ToInt32(row["Punteros"]) : 0,
                        personasMovilizadas = row["PersonasMovilizadas"] != DBNull.Value ? Convert.ToInt32(row["PersonasMovilizadas"]) : 0
                    });
                }

                return Ok(new { exito = 1, dato = lista, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpGet("admin-ranking-movilizadores")]
        public IActionResult AdminRankingMovilizadores(string idUsuario)
        {
            try
            {
                var ds = _service.AdminRankingMovilizadores(idUsuario);
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpGet("admin-ranking-zonas")]
        public IActionResult AdminRankingZonas(string idUsuario)
        {
            try
            {
                var ds = _service.AdminRankingZonas( idUsuario);
                return Ok(new { exito = 1, dato = ds, status = "ok" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { exito = 0, dato = (object?)null, status = ex.Message });
            }
        }

        [HttpGet("admin-diad-por-zona")]
        public IActionResult AdminDiaDPorZona(string idUsuario)
        {
            try
            {
                var ds = _service.AdminDiaDPorZona( idUsuario);
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
        public IActionResult AdminComparativoZonas(string idUsuario)
        {
            try
            {
                var ds = _service.AdminComparativoZonas( idUsuario);

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
        public IActionResult AdminComparativoGerentes(string idUsuario)
        {
            try
            {
                var ds = _service.AdminComparativoGerentes(idUsuario);

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
        public IActionResult AdminRankingZonasExcel(string idUsuario)
        {
            try
            {
                var ds = _service.AdminRankingZonas( idUsuario);

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
        public IActionResult AdminRankingMovilizadoresExcel(string idUsuario)
        {
            try
            {
                var ds = _service.AdminRankingMovilizadores( idUsuario);

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
        public IActionResult AdminDiaDPorZonaExcel(string idUsuario)
        {
            try
            {
                var ds = _service.AdminDiaDPorZona( idUsuario);

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

        [HttpGet("superadmin-resumen-municipios-excel")]
        public IActionResult SuperAdminResumenMunicipiosExcel()
        {
            try
            {
                var dt = _service.SuperAdminResumenMunicipios();

                if (dt == null || dt.Rows.Count == 0)
                {
                    return BadRequest(new
                    {
                        exito = 0,
                        dato = (object?)null,
                        status = "No hay datos para exportar"
                    });
                }

                // Formatear celulares para Paraguay en el DataTable antes de exportar
                if (dt.Columns.Contains("Celular"))
                {
                    foreach (System.Data.DataRow row in dt.Rows)
                    {
                        if (row["Celular"] != DBNull.Value)
                        {
                            row["Celular"] = FormatearCelularParaguay(row["Celular"].ToString());
                        }
                    }
                }

                return _excelExportService.ExportarXlsx(
                    dt,
                    "Resumen Municipios",
                    "resumen_municipios.xlsx",
                    ("Municipio", "Municipio"),
                    ("Administrador", "Administrador"),
                    ("Celular Administrador", "Celular"),
                    ("Concejales (Rol 2)", "Concejales"),
                    ("Punteros (Rol 3)", "Punteros"),
                    ("Personas Movilizadas", "PersonasMovilizadas")
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

        private static string FormatearCelularParaguay(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            var digits = new string(raw.Where(char.IsDigit).ToArray());
            if (digits.StartsWith("595") && digits.Length == 12) digits = digits.Substring(3);
            else if (digits.StartsWith("0") && digits.Length == 10) digits = digits.Substring(1);

            if (digits.Length == 9 && digits.StartsWith("9"))
            {
                return $"+595 {digits.Substring(0, 3)} {digits.Substring(3, 3)} {digits.Substring(6)}";
            }
            return raw.Trim();
        }
    }

}