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
        public DataSet AdminDiaDResumen(string? idUsuario = null, int? horaInicio = null, int? horaFin = null)
        {
            return EjecutarPA_DS(
                "PA_DASHBOARD_DIAD_RESUMEN",
                new SqlParameter("@IdUsuario", SqlDbType.VarChar, 50) { Value = (object?)idUsuario ?? DBNull.Value },
                new SqlParameter("@HoraInicio", SqlDbType.Int) { Value = (object?)horaInicio ?? DBNull.Value },
                new SqlParameter("@HoraFin", SqlDbType.Int) { Value = (object?)horaFin ?? DBNull.Value }
            );
        }
        public DataSet AdminControlElectoralResumen(string? idUsuario = null, int? horaInicio = null, int? horaFin = null)
        {
            return EjecutarPA_DS(
                "PA_DASHBOARD_CONTROL_ELECTORAL_RESUMEN",
                new SqlParameter("@IdUsuario", SqlDbType.VarChar, 50) { Value = (object?)idUsuario ?? DBNull.Value },
                new SqlParameter("@HoraInicio", SqlDbType.Int) { Value = (object?)horaInicio ?? DBNull.Value },
                new SqlParameter("@HoraFin", SqlDbType.Int) { Value = (object?)horaFin ?? DBNull.Value }
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
WITH VotantesDiaD AS (
    SELECT 
        rec.IdMunicipio,
        SUM(CASE WHEN vot.EstadoDiaD = 'YA_VOTO' THEN 1 ELSE 0 END) AS VotosPadronGeneral,
        SUM(CASE WHEN vot.PasoPorElPC = 1 THEN 1 ELSE 0 END) AS TotalPasoPC,
        SUM(CASE WHEN vot.PasoPorElPC = 1 AND (vot.EstadoDiaD IS NULL OR vot.EstadoDiaD <> 'YA_VOTO') THEN 1 ELSE 0 END) AS TotalPcNoVoto,
        SUM(CASE WHEN vot.PasoPorElPC = 1 AND vot.EstadoDiaD = 'YA_VOTO' THEN 1 ELSE 0 END) AS TotalPcSiVoto,
        SUM(CASE WHEN vot.Combustible = 1 THEN 1 ELSE 0 END) AS TotalCombustible,
        COUNT(DISTINCT CASE WHEN vot.EstadoDiaD = 'YA_VOTO' OR vot.PasoPorElPC = 1 THEN vot.CI ELSE NULL END) AS TotalDiaDMasPC
    FROM dbo.TB_Votante vot WITH (NOLOCK)
    INNER JOIN dbo.TB_Recinto rec WITH (NOLOCK) ON rec.IdRecinto = vot.IdRecintoOk
    WHERE vot.EstadoDiaD = 'YA_VOTO' OR vot.PasoPorElPC = 1 OR vot.Combustible = 1
    GROUP BY rec.IdMunicipio
)
select 
    u.NombreCompleto as Administrador, 
    u.Celular as Celular,
    t.Nombre as Municipio,
    t.IdTerritorio as IdMunicipio,
    (select count(*) 
     from Usuario g with (nolock) 
     where g.IdUsuarioSupervisor = u.IdUsuario and g.Activo = 1 and g.IdRol = 2) as Concejales,
    (select count(*) 
     from Usuario g1 with (nolock) 
     join Usuario m with (nolock) on m.IdUsuarioSupervisor = g1.IdUsuario and m.Activo = 1 and m.IdRol = 3
     where g1.IdUsuarioSupervisor = u.IdUsuario and g1.Activo = 1 and g1.IdRol = 2) as Punteros,
    (select count(distinct v.IdUsuario)
     from Usuario v with (nolock)
     left join Territorio tv with (nolock) on tv.IdTerritorio = v.IdTerritorio
     left join Usuario gv with (nolock) on gv.IdUsuario = v.IdUsuarioSupervisor
     where v.Activo = 1 and v.IdRol = 1003
       and (
           v.IdTerritorio = t.IdTerritorio 
           OR tv.IdTerritorioPadre = t.IdTerritorio
           OR v.IdUsuarioSupervisor = u.IdUsuario
           OR gv.IdUsuarioSupervisor = u.IdUsuario
           OR v.IdUsuarioCreate = u.IdUsuario
       )) as Veedores,
    (select count(*) 
     from Usuario g1 with (nolock) 
     join Usuario m with (nolock) on m.IdUsuarioSupervisor = g1.IdUsuario and m.Activo = 1 and m.IdRol = 3
     join PersonaMovilizada pm with (nolock) on pm.IdUsuarioMovilizador = m.IdUsuario and (pm.Activo is null or pm.Activo = 1)
     where g1.IdUsuarioSupervisor = u.IdUsuario and g1.Activo = 1 and g1.IdRol = 2) as PersonasMovilizadas,
    (select count(*) 
     from Usuario g1 with (nolock) 
     join Usuario m with (nolock) on m.IdUsuarioSupervisor = g1.IdUsuario and m.Activo = 1 and m.IdRol = 3
     join PersonaMovilizada pm with (nolock) on pm.IdUsuarioMovilizador = m.IdUsuario and (pm.Activo is null or pm.Activo = 1)
     where g1.IdUsuarioSupervisor = u.IdUsuario and g1.Activo = 1 and g1.IdRol = 2 and cast(pm.FechaRegistro as date) = cast(getdate() as date)) as PersonasHoy,
    (select count(*) 
     from Usuario g1 with (nolock) 
     join Usuario m with (nolock) on m.IdUsuarioSupervisor = g1.IdUsuario and m.Activo = 1 and m.IdRol = 3
     join PersonaMovilizada pm with (nolock) on pm.IdUsuarioMovilizador = m.IdUsuario and (pm.Activo is null or pm.Activo = 1)
     where g1.IdUsuarioSupervisor = u.IdUsuario and g1.Activo = 1 and g1.IdRol = 2 and cast(pm.FechaRegistro as date) = cast(dateadd(day, -1, getdate()) as date)) as PersonasAyer,
    (select count(*) 
     from Usuario g1 with (nolock) 
     join Usuario m with (nolock) on m.IdUsuarioSupervisor = g1.IdUsuario and m.Activo = 1 and m.IdRol = 3
     join PersonaMovilizada pm with (nolock) on pm.IdUsuarioMovilizador = m.IdUsuario and (pm.Activo is null or pm.Activo = 1)
     where g1.IdUsuarioSupervisor = u.IdUsuario and g1.Activo = 1 and g1.IdRol = 2 and pm.EstadoDiaD = 'YA_VOTO') as YaVoto,
    (select count(*) 
     from Usuario g1 with (nolock) 
     join Usuario m with (nolock) on m.IdUsuarioSupervisor = g1.IdUsuario and m.Activo = 1 and m.IdRol = 3
     join PersonaMovilizada pm with (nolock) on pm.IdUsuarioMovilizador = m.IdUsuario and (pm.Activo is null or pm.Activo = 1)
     where g1.IdUsuarioSupervisor = u.IdUsuario and g1.Activo = 1 and g1.IdRol = 2 and pm.EstadoDiaD = 'NO_CONTACTADO') as NoContactado,
    ISNULL(vd.VotosPadronGeneral, 0) as VotosPadronGeneral,
    ISNULL(vd.TotalPasoPC, 0) as TotalPasoPC,
    ISNULL(vd.TotalPcNoVoto, 0) as TotalPcNoVoto,
    ISNULL(vd.TotalPcSiVoto, 0) as TotalPcSiVoto,
    ISNULL(vd.TotalCombustible, 0) as TotalCombustible,
    ISNULL(vd.TotalDiaDMasPC, 0) as TotalDiaDMasPC
from Usuario u with (nolock)
join Territorio t with (nolock) on u.IdTerritorio = t.IdTerritorio and t.Activo = 1
left join VotantesDiaD vd on vd.IdMunicipio = t.IdTerritorio
where u.Activo = 1 and u.IdRol = 1 and u.IdTerritorio is not null and u.NombreCompleto not like '%madrid%'
order by t.Nombre, u.NombreCompleto";

            return EjecutarSQL(sql);
        }
    }
}