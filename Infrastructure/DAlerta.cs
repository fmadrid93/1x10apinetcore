using System.Data;
using Microsoft.Data.SqlClient;

namespace Infrastructure
{
    public class DAlerta : DbHelper
    {
        public DataTable GenerarMetaBaja()
        {
            return EjecutarPA("pa_alerta_generar_meta_baja");
        }

        public DataTable GenerarSinReporteDiaD()
        {
            return EjecutarPA("pa_alerta_generar_sin_reporte_diad");
        }

        public DataTable ListarPorUsuario(int idUsuario)
        {
            return EjecutarPA(
                "pa_alerta_listar_por_usuario",
                new SqlParameter("@IdUsuario", SqlDbType.Int) { Value = idUsuario }
            );
        }

        public DataTable Atender(int idAlerta)
        {
            return EjecutarPA(
                "pa_alerta_atender",
                new SqlParameter("@IdAlerta", SqlDbType.Int) { Value = idAlerta }
            );
        }

        public DataTable GenerarSinCarga()
        {
            return EjecutarPA("pa_alerta_generar_sin_carga");
        }

        public DataTable GenerarBajoMeta()
        {
            return EjecutarPA("pa_alerta_generar_bajo_meta");
        }


        public DataTable ListarPorGerente(int idGerente)
        {
            return EjecutarPA(
                "pa_alerta_listar_por_gerente",
                new SqlParameter("@IdGerente", SqlDbType.Int) { Value = idGerente }
            );
        }
        public DataTable GenerarAvanceLento()
        {
            return EjecutarPA("pa_alerta_generar_avance_lento");
        }

    }
}