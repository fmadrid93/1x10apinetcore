using System.Data;
using Microsoft.Data.SqlClient;

namespace Infrastructure
{
    public class DDashboard : DbHelper
    {
        public DataTable AdminKpis(string idUsuario)
        {
            return EjecutarPA("pa_dashboard_admin_kpis",
                new SqlParameter("@IdUsuario", SqlDbType.Int) { Value = idUsuario });
        }

        public DataTable AdminRankingMovilizadores(string idUsuario)
        {
            return EjecutarPA("pa_dashboard_admin_ranking_movilizadores",
                new SqlParameter("@IdUsuario", SqlDbType.Int) { Value = idUsuario });
        }

        public DataTable AdminRankingZonas(string idUsuario)
        {
            return EjecutarPA("pa_dashboard_admin_ranking_zonas",
                new SqlParameter("@IdUsuario", SqlDbType.Int) { Value = idUsuario });
        }

        public DataTable AdminDiaDPorZona(string idUsuario)
        {
            return EjecutarPA("pa_dashboard_admin_diad_por_zona",
                new SqlParameter("@IdUsuario", SqlDbType.Int) { Value = idUsuario });
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
        public DataTable AdminComparativoZonas(string idUsuario)
        {
            return EjecutarPA (
                "PA_DASHBOARD_ADMIN_COMPARATIVO_ZONAS",
                new SqlParameter("@IdUsuario", SqlDbType.VarChar, 50) { Value = idUsuario }
            );
        }
        public DataTable AdminComparativoGerentes(string idUsuario)
        {
            return EjecutarPA(
                "PA_DASHBOARD_ADMIN_COMPARATIVO_GERENTES",
                new SqlParameter("@IdUsuario", SqlDbType.VarChar,50) { Value = idUsuario }
            );
        }

        public DataTable SuperAdminResumenMunicipios()
        {
            string sql = @"
select 
    u.NombreCompleto as Administrador, 
    t.Nombre as Municipio,
    (select count(*) 
     from Usuario g with (nolock) 
     where g.IdUsuarioSupervisor = u.IdUsuario and g.Activo = 1 and g.IdRol = 2) as Concejales,
    (select count(*) 
     from Usuario g1 with (nolock) 
     join Usuario m with (nolock) on m.IdUsuarioSupervisor = g1.IdUsuario and m.Activo = 1 and m.IdRol = 3
     where g1.IdUsuarioSupervisor = u.IdUsuario and g1.Activo = 1 and g1.IdRol = 2) as Punteros,
    (select count(*) 
     from Usuario g1 with (nolock) 
     join Usuario m with (nolock) on m.IdUsuarioSupervisor = g1.IdUsuario and m.Activo = 1 and m.IdRol = 3
     join PersonaMovilizada pm with (nolock) on pm.IdUsuarioMovilizador = m.IdUsuario and (pm.Activo is null or pm.Activo = 1)
     where g1.IdUsuarioSupervisor = u.IdUsuario and g1.Activo = 1 and g1.IdRol = 2) as PersonasMovilizadas
from Usuario u with (nolock)
join Territorio t with (nolock) on u.IdTerritorio = t.IdTerritorio and t.Activo = 1
where u.Activo = 1 and u.IdRol = 1 and u.IdTerritorio is not null and u.NombreCompleto not like '%madrid%'
order by t.Nombre, u.NombreCompleto";

            return EjecutarSQL(sql);
        }
    }
}