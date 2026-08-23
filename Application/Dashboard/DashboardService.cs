using System.Data;
using Infrastructure;

namespace Application.Dashboard
{
    public class DashboardService
    {
        private readonly DDashboard _data = new DDashboard();

        public DataTable AdminKpis(string idUsuario)
        {
            return _data.AdminKpis( idUsuario);
        }

        public DataTable AdminRankingMovilizadores(string idUsuario)
        {
            return _data.AdminRankingMovilizadores(idUsuario);
        }

        public DataTable AdminRankingZonas(string idUsuario)
        {
            return _data.AdminRankingZonas( idUsuario);
        }

        public DataTable AdminDiaDPorZona(string idUsuario)
        {
            return _data.AdminDiaDPorZona( idUsuario);
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
        public DataTable AdminComparativoZonas(string idUsuario)
        {
            return _data.AdminComparativoZonas( idUsuario);
        }
        public DataTable AdminComparativoGerentes(string idUsuario)
        {
            return _data.AdminComparativoGerentes(idUsuario);
        }

        public DataTable SuperAdminResumenMunicipios()
        {
            return _data.SuperAdminResumenMunicipios();
        }
    }
}