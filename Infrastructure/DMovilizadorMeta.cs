using System.Data;
using Microsoft.Data.SqlClient;

namespace Infrastructure
{
    public class DMovilizadorMeta : DbHelper
    {
        public DataTable Listar()
        {
            return EjecutarPA("pa_movilizador_meta_listar");
        }

        public DataTable Obtener(int idUsuarioMovilizador)
        {
            return EjecutarPA(
                "pa_movilizador_meta_obtener",
                new SqlParameter("@IdUsuarioMovilizador", SqlDbType.Int) { Value = idUsuarioMovilizador }
            );
        }

        public DataTable Guardar(int idUsuarioMovilizador, int metaObjetivo)
        {
            return EjecutarPA(
                "pa_movilizador_meta_guardar",
                new SqlParameter("@IdUsuarioMovilizador", SqlDbType.Int) { Value = idUsuarioMovilizador },
                new SqlParameter("@MetaObjetivo", SqlDbType.Int) { Value = metaObjetivo }
            );
        }
        public DataTable ListarPorGerente(int idGerente)
        {
            return EjecutarPA(
                "pa_movilizador_meta_listar_por_gerente",
                new SqlParameter("@IdGerente", SqlDbType.Int) { Value = idGerente }
            );
        }
    }
}