using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace Infrastructure
{
    public class DNotificacion : DbHelper
    {
        public DataTable RegistrarToken(int idUsuario, string token, string? plataforma)
        {
            return EjecutarPA(
                "pa_notificacion_registrar_token",
                new SqlParameter("@IdUsuario", SqlDbType.Int) { Value = idUsuario },
                new SqlParameter("@Token", SqlDbType.NVarChar, 500) { Value = token },
                new SqlParameter("@Plataforma", SqlDbType.VarChar, 20) { Value = (object?)plataforma ?? DBNull.Value }
            );
        }

        public DataTable ObtenerTokenActivoPorUsuario(int idUsuario)
        {
            return EjecutarPA(
                "pa_notificacion_obtener_token_activo_por_usuario",
                new SqlParameter("@IdUsuario", SqlDbType.Int) { Value = idUsuario }
            );
        }
    }
}