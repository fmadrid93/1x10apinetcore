using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace Infrastructure
{
    public class DDiaD : DbHelper
    {
        public DataTable MarcarEstado(int idPersonaMovilizada, int idUsuarioMarca, string estadoDiaD, string? observacion)
        {
            return EjecutarPA(
                "pa_diad_marcar_estado",
                new SqlParameter("@IdPersonaMovilizada", SqlDbType.Int) { Value = idPersonaMovilizada },
                new SqlParameter("@IdUsuarioMarca", SqlDbType.Int) { Value = idUsuarioMarca },
                new SqlParameter("@EstadoDiaD", SqlDbType.VarChar, 30) { Value = estadoDiaD },
                new SqlParameter("@Observacion", SqlDbType.VarChar, 250) { Value = (object?)observacion ?? DBNull.Value }
            );
        }

        public DataTable HistorialPorPersona(int idPersonaMovilizada)
        {
            return EjecutarPA(
                "pa_diad_historial_por_persona",
                new SqlParameter("@IdPersonaMovilizada", SqlDbType.Int) { Value = idPersonaMovilizada }
            );
        }

        public DataTable ListarPorMovilizador(int idUsuarioMovilizador)
        {
            return EjecutarPA(
                "pa_diad_listar_por_movilizador",
                new SqlParameter("@IdUsuarioMovilizador", SqlDbType.Int) { Value = idUsuarioMovilizador }
            );
        }

        public DataTable ResumenAdmin()
        {
            return EjecutarPA("pa_diad_resumen_admin");
        }

        public DataTable ResumenGerente(int idGerente)
        {
            return EjecutarPA(
                "pa_diad_resumen_gerente",
                new SqlParameter("@IdGerente", SqlDbType.Int) { Value = idGerente }
            );
        }

        public DataTable VelocidadVotacion30Min()
        {
            return EjecutarPA("pa_diad_velocidad_votacion_30min");
        }

        public DataTable CurvaAvance()
        {
            return EjecutarPA("pa_diad_curva_avance");
        }

        public DataTable BrechaMetaHora()
        {
            return EjecutarPA("pa_diad_brecha_meta_hora");
        }
    }
}