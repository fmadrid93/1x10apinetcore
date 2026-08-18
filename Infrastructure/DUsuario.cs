using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace Infrastructure
{
    public class DUsuario : DbHelper
    {
        public DataTable Insertar(
            int idRol,
            int? idTerritorio,
            int? idUsuarioSupervisor,
            string usuario,
            string claveHash,
            string nombreCompleto,
            string? ci,
            string? celular,
            string? email,
            int? idUsuarioCreate)
        {
            return EjecutarPA(
                "pa_usuario_insertar",
                new SqlParameter("@IdRol", SqlDbType.Int) { Value = idRol },
                new SqlParameter("@IdTerritorio", SqlDbType.Int) { Value = (object?)idTerritorio ?? DBNull.Value },
                new SqlParameter("@IdUsuarioSupervisor", SqlDbType.Int) { Value = (object?)idUsuarioSupervisor ?? DBNull.Value },
                new SqlParameter("@Usuario", SqlDbType.VarChar, 50) { Value = usuario },
                new SqlParameter("@ClaveHash", SqlDbType.VarChar, 300) { Value = claveHash },
                new SqlParameter("@NombreCompleto", SqlDbType.VarChar, 200) { Value = nombreCompleto },
                new SqlParameter("@CI", SqlDbType.VarChar, 30) { Value = (object?)ci ?? DBNull.Value },
                new SqlParameter("@Celular", SqlDbType.VarChar, 30) { Value = (object?)celular ?? DBNull.Value },
                new SqlParameter("@Email", SqlDbType.VarChar, 150) { Value = (object?)email ?? DBNull.Value },
                new SqlParameter("@IdUsuarioCreate", SqlDbType.Int) { Value = (object?)idUsuarioCreate ?? DBNull.Value }
            );
        }

        public DataTable Actualizar(
            int idUsuario,
            int idRol,
            int? idTerritorio,
            int? idUsuarioSupervisor,
            string nombreCompleto,
            string? ci,
            string? celular,
            string? email,
            bool activo)
        {
            return EjecutarPA(
                "pa_usuario_actualizar",
                new SqlParameter("@IdUsuario", SqlDbType.Int) { Value = idUsuario },
                new SqlParameter("@IdRol", SqlDbType.Int) { Value = idRol },
                new SqlParameter("@IdTerritorio", SqlDbType.Int) { Value = (object?)idTerritorio ?? DBNull.Value },
                new SqlParameter("@IdUsuarioSupervisor", SqlDbType.Int) { Value = (object?)idUsuarioSupervisor ?? DBNull.Value },
                new SqlParameter("@NombreCompleto", SqlDbType.VarChar, 200) { Value = nombreCompleto },
                new SqlParameter("@CI", SqlDbType.VarChar, 30) { Value = (object?)ci ?? DBNull.Value },
                new SqlParameter("@Celular", SqlDbType.VarChar, 30) { Value = (object?)celular ?? DBNull.Value },
                new SqlParameter("@Email", SqlDbType.VarChar, 150) { Value = (object?)email ?? DBNull.Value },
                new SqlParameter("@Activo", SqlDbType.Bit) { Value = activo }
            );
        }

        public DataTable CambiarClave(int idUsuario, string claveHash)
        {
            return EjecutarPA(
                "pa_usuario_cambiar_clave",
                new SqlParameter("@IdUsuario", SqlDbType.Int) { Value = idUsuario },
                new SqlParameter("@ClaveHash", SqlDbType.VarChar, 300) { Value = claveHash }
            );
        }

        public DataTable EliminarLogico(int idUsuario)
        {
            return EjecutarPA(
                "pa_usuario_eliminar_logico",
                new SqlParameter("@IdUsuario", SqlDbType.Int) { Value = idUsuario }
            );
        }

        public DataTable Listar(int? idRol, int? idTerritorio, int? idUsuarioSupervisor, bool soloActivos, int? idUsuarioCreate)
        {
            return EjecutarPA(
                "pa_usuario_listar",
                new SqlParameter("@IdRol", SqlDbType.Int) { Value = (object?)idRol ?? DBNull.Value },
                new SqlParameter("@IdTerritorio", SqlDbType.Int) { Value = (object?)idTerritorio ?? DBNull.Value },
                new SqlParameter("@IdUsuarioSupervisor", SqlDbType.Int) { Value = (object?)idUsuarioSupervisor ?? DBNull.Value },
                new SqlParameter("@SoloActivos", SqlDbType.Bit) { Value = soloActivos },
                new SqlParameter("@IdUsuarioCreate", SqlDbType.Int) { Value = (object?)idUsuarioCreate ?? DBNull.Value }
            );
        }

        public DataTable ObtenerPorId(int idUsuario)
        {
            return EjecutarPA(
                "pa_usuario_obtener_por_id",
                new SqlParameter("@IdUsuario", SqlDbType.Int) { Value = idUsuario }
            );
        }

        public DataTable ObtenerParaLogin(string usuario)
        {
            return EjecutarPA(
                "pa_usuario_obtener_para_login",
                new SqlParameter("@Usuario", SqlDbType.VarChar, 50) { Value = usuario }
            );
        }
    }
}