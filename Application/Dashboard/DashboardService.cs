using System.Data;
using Infrastructure;

namespace Application.Dashboard
{
    public class DashboardService
    {
        private readonly DDashboard _data = new DDashboard();

        public DataTable AdminKpis()
        {
            return _data.AdminKpis();
        }

        public DataTable AdminRankingMovilizadores()
        {
            return _data.AdminRankingMovilizadores();
        }

        public DataTable AdminRankingZonas()
        {
            return _data.AdminRankingZonas();
        }

        public DataTable AdminDiaDPorZona()
        {
            return _data.AdminDiaDPorZona();
        }

        public DataTable GerenteKpis(int idGerente)
        {
            return _data.GerenteKpis(idGerente);
        }

        public DataTable GerenteRankingMovilizadores(int idGerente)
        {
            return _data.GerenteRankingMovilizadores(idGerente);
        }

        public DataTable GerenteAlertas(int idGerente)
        {
            return _data.GerenteAlertas(idGerente);
        }
        public DataTable AdminDetalleZona(int idTerritorio)
        {
            return _data.AdminDetalleZona(idTerritorio);
        }
        public DataSet AdminDiaDResumen(int? horaInicio = null, int? horaFin = null)
        {
            return _data.AdminDiaDResumen( horaInicio,  horaFin );
        }
        public DataSet GerenteDiaDResumen(int idGerente, int? horaInicio = null, int? horaFin = null)
        {
            return _data.GerenteDiaDResumen(idGerente, horaInicio ,  horaFin );
        }
        public DataTable AdminComparativoZonas()
        {
            return _data.AdminComparativoZonas();
        }
        public DataTable AdminComparativoGerentes()
        {
            return _data.AdminComparativoGerentes();
        }
    }
}