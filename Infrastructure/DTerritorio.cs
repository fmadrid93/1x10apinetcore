using System;
using System.Data;
using Infraestructure;
using Microsoft.Data.SqlClient;

namespace Infrastructure
{
    public class DTerritorio : DbHelper
    {
        private static bool _columnaVerificada = false;

        private void AsegurarColumnaUrlWhatsApp()
        {
            if (_columnaVerificada) return;
            try
            {
                string sql = @"
                    IF NOT EXISTS (
                        SELECT 1 FROM sys.columns 
                        WHERE object_id = OBJECT_ID('Territorio') 
                          AND name = 'UrlServidorWhatsApp'
                    )
                    BEGIN
                        ALTER TABLE Territorio ADD UrlServidorWhatsApp VARCHAR(500) NULL;
                    END";

                abrirConexion();
                using (SqlCommand cmd = new SqlCommand(sql, obtenerConexion()))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandTimeout = 30;
                    cmd.ExecuteNonQuery();
                }
                _columnaVerificada = true;
            }
            catch
            {
                // Ignorar si ya existe o no hay permisos DDL
            }
            finally
            {
                cerrarConexion();
            }
        }

        public DataTable Insertar(int? idTerritorioPadre, string nombre, string tipoTerritorio, string? codigo, string? urlServidorWhatsApp = null)
        {
            AsegurarColumnaUrlWhatsApp();

            DataTable dt = EjecutarPA(
                "pa_territorio_insertar",
                new SqlParameter("@IdTerritorioPadre", SqlDbType.Int) { Value = (object?)idTerritorioPadre ?? DBNull.Value },
                new SqlParameter("@Nombre", SqlDbType.VarChar, 150) { Value = nombre },
                new SqlParameter("@TipoTerritorio", SqlDbType.VarChar, 50) { Value = tipoTerritorio },
                new SqlParameter("@Codigo", SqlDbType.VarChar, 50) { Value = (object?)codigo ?? DBNull.Value }
            );

            if (!string.IsNullOrEmpty(urlServidorWhatsApp) && dt != null && dt.Rows.Count > 0)
            {
                try
                {
                    int idNuevo = Convert.ToInt32(dt.Rows[0]["IdTerritorio"]);
                    ActualizarUrlServidorWhatsAppDirecto(idNuevo, urlServidorWhatsApp);
                }
                catch { }
            }

            return dt;
        }

        public DataTable Listar(bool soloActivos)
        {
            AsegurarColumnaUrlWhatsApp();

            string sql = @"
                SELECT 
                    t.IdTerritorio,
                    t.IdTerritorioPadre,
                    t.Nombre,
                    t.TipoTerritorio,
                    t.Codigo,
                    t.Activo,
                    t.UrlServidorWhatsApp,
                    tp.Nombre AS NombrePadre
                FROM Territorio t WITH (NOLOCK)
                LEFT JOIN Territorio tp WITH (NOLOCK) ON t.IdTerritorioPadre = tp.IdTerritorio
                WHERE (@SoloActivos = 0 OR t.Activo = 1)
                ORDER BY t.TipoTerritorio, t.Nombre";

            DataSet ds = new DataSet();
            try
            {
                abrirConexion();
                using (SqlCommand cmd = new SqlCommand(sql, obtenerConexion()))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandTimeout = 60;
                    cmd.Parameters.Add(new SqlParameter("@SoloActivos", SqlDbType.Bit) { Value = soloActivos });
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(ds);
                    }
                }
            }
            catch
            {
                // Fallback al PA original si hay incompatibilidad
                return EjecutarPA("pa_territorio_listar", new SqlParameter("@SoloActivos", SqlDbType.Bit) { Value = soloActivos });
            }
            finally
            {
                cerrarConexion();
            }

            return ds.Tables.Count > 0 ? ds.Tables[0] : new DataTable();
        }

        public DataTable ObtenerPorId(int idTerritorio)
        {
            AsegurarColumnaUrlWhatsApp();

            string sql = @"
                SELECT 
                    t.IdTerritorio,
                    t.IdTerritorioPadre,
                    t.Nombre,
                    t.TipoTerritorio,
                    t.Codigo,
                    t.Activo,
                    t.UrlServidorWhatsApp,
                    tp.Nombre AS NombrePadre
                FROM Territorio t WITH (NOLOCK)
                LEFT JOIN Territorio tp WITH (NOLOCK) ON t.IdTerritorioPadre = tp.IdTerritorio
                WHERE t.IdTerritorio = @IdTerritorio";

            DataSet ds = new DataSet();
            try
            {
                abrirConexion();
                using (SqlCommand cmd = new SqlCommand(sql, obtenerConexion()))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandTimeout = 60;
                    cmd.Parameters.Add(new SqlParameter("@IdTerritorio", SqlDbType.Int) { Value = idTerritorio });
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(ds);
                    }
                }
            }
            catch
            {
                return EjecutarPA("pa_territorio_obtener_por_id", new SqlParameter("@IdTerritorio", SqlDbType.Int) { Value = idTerritorio });
            }
            finally
            {
                cerrarConexion();
            }

            return ds.Tables.Count > 0 ? ds.Tables[0] : new DataTable();
        }

        public DataTable Actualizar(int idTerritorio, int? idTerritorioPadre, string nombre, string tipoTerritorio, string? codigo, bool activo, string? urlServidorWhatsApp = null)
        {
            AsegurarColumnaUrlWhatsApp();

            DataTable dt = EjecutarPA(
                "pa_territorio_actualizar",
                new SqlParameter("@IdTerritorio", SqlDbType.Int) { Value = idTerritorio },
                new SqlParameter("@IdTerritorioPadre", SqlDbType.Int) { Value = (object?)idTerritorioPadre ?? DBNull.Value },
                new SqlParameter("@Nombre", SqlDbType.VarChar, 150) { Value = nombre },
                new SqlParameter("@TipoTerritorio", SqlDbType.VarChar, 50) { Value = tipoTerritorio },
                new SqlParameter("@Codigo", SqlDbType.VarChar, 50) { Value = (object?)codigo ?? DBNull.Value },
                new SqlParameter("@Activo", SqlDbType.Bit) { Value = activo }
            );

            try
            {
                ActualizarUrlServidorWhatsAppDirecto(idTerritorio, urlServidorWhatsApp);
            }
            catch { }

            return dt;
        }

        public void ActualizarUrlServidorWhatsAppDirecto(int idTerritorio, string? url)
        {
            string sql = "UPDATE Territorio SET UrlServidorWhatsApp = @Url WHERE IdTerritorio = @IdTerritorio";
            try
            {
                abrirConexion();
                using (SqlCommand cmd = new SqlCommand(sql, obtenerConexion()))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.Add(new SqlParameter("@IdTerritorio", SqlDbType.Int) { Value = idTerritorio });
                    cmd.Parameters.Add(new SqlParameter("@Url", SqlDbType.VarChar, 500) { Value = (object?)url ?? DBNull.Value });
                    cmd.ExecuteNonQuery();
                }
            }
            finally
            {
                cerrarConexion();
            }
        }
    }
}