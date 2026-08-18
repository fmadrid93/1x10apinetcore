using System.Data;
using Microsoft.Data.SqlClient;

namespace Infrastructure
{
    public class DDashboard : DbHelper
    {
        public DataTable AdminKpis()
        {
            return EjecutarPA("pa_dashboard_admin_kpis");
        }

        public DataTable AdminRankingMovilizadores()
        {
            return EjecutarPA("pa_dashboard_admin_ranking_movilizadores");
        }

        public DataTable AdminRankingZonas()
        {
            return EjecutarPA("pa_dashboard_admin_ranking_zonas");
        }

        public DataTable AdminDiaDPorZona()
        {
            return EjecutarPA("pa_dashboard_admin_diad_por_zona");
        }

        public DataTable GerenteKpis(int idGerente)
        {
            return EjecutarPA(
                "pa_dashboard_gerente_kpis",
                new SqlParameter("@IdGerente", SqlDbType.Int) { Value = idGerente }
            );
        }

        public DataTable GerenteRankingMovilizadores(int idGerente)
        {
            return EjecutarPA(
                "pa_dashboard_gerente_ranking_movilizadores",
                new SqlParameter("@IdGerente", SqlDbType.Int) { Value = idGerente }
            );
        }

        public DataTable GerenteAlertas(int idGerente)
        {
            return EjecutarPA(
                "pa_dashboard_gerente_alertas",
                new SqlParameter("@IdGerente", SqlDbType.Int) { Value = idGerente }
            );
        }
        public DataTable AdminDetalleZona(int idTerritorio)
        {
            return EjecutarPA(
                "pa_dashboard_admin_detalle_zona",
                new SqlParameter("@IdTerritorio", SqlDbType.Int) { Value = idTerritorio }
            );
        }
        public DataSet AdminDiaDResumen(int? horaInicio=null, int? horaFin=null)
        {
            return EjecutarPA_DS(
                "PA_DASHBOARD_DIAD_RESUMEN",
                new SqlParameter("@HoraInicio", SqlDbType.Int) { Value = horaInicio },
                new SqlParameter("@HoraFin", SqlDbType.Int) { Value = horaFin }
            );
        }
        public DataSet GerenteDiaDResumen(int idGerente, int? horaInicio = null, int? horaFin = null)
        {
            return EjecutarPA_DS(
                "PA_DASHBOARD_GERENTE_DIAD_RESUMEN",
                new SqlParameter("@IdGerente", SqlDbType.Int) { Value = idGerente },
                new SqlParameter("@HoraInicio", SqlDbType.Int) { Value = horaInicio },
                new SqlParameter("@HoraFin", SqlDbType.Int) { Value = horaFin }
            );
        }
        public DataTable AdminComparativoZonas()
        {
            return EjecutarPA (
                "PA_DASHBOARD_ADMIN_COMPARATIVO_ZONAS"
            );
        }
        public DataTable AdminComparativoGerentes()
        {
            return EjecutarPA(
                "PA_DASHBOARD_ADMIN_COMPARATIVO_GERENTES"
            );
        }
    }
}