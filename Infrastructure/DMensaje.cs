using System.Data;
using Microsoft.Data.SqlClient;

namespace Infrastructure
{
    public class DMensaje : DbHelper
    {
        public DataTable Enviar(int idUsuarioEmisor, int idUsuarioDestino, string titulo, string contenido)
        {
            return EjecutarPA(
                "pa_mensaje_enviar",
                new SqlParameter("@IdUsuarioEmisor", SqlDbType.Int) { Value = idUsuarioEmisor },
                new SqlParameter("@IdUsuarioDestino", SqlDbType.Int) { Value = idUsuarioDestino },
                new SqlParameter("@Titulo", SqlDbType.VarChar, 150) { Value = titulo },
                new SqlParameter("@Contenido", SqlDbType.VarChar, 500) { Value = contenido }
            );
        }
            public DataTable ListarRecibidos(int idUsuarioDestino)
            {
                return EjecutarPA(
                    "pa_mensaje_listar_recibidos",
                    new SqlParameter("@IdUsuarioDestino", SqlDbType.Int) { Value = idUsuarioDestino }
                );
            }

            public DataTable ListarEnviados(int idUsuarioEmisor)
            {
                return EjecutarPA(
                    "pa_mensaje_listar_enviados",
                    new SqlParameter("@IdUsuarioEmisor", SqlDbType.Int) { Value = idUsuarioEmisor }
                );
            }

    public DataTable ListarBandeja(int idUsuarioDestino)
        {
            return EjecutarPA(
                "pa_mensaje_listar_bandeja",
                new SqlParameter("@IdUsuarioDestino", SqlDbType.Int) { Value = idUsuarioDestino }
            );
        }

        public DataTable MarcarLeido(int idMensaje)
        {
            return EjecutarPA(
                "pa_mensaje_marcar_leido",
                new SqlParameter("@IdMensaje", SqlDbType.Int) { Value = idMensaje }
            );
        }
    }
}