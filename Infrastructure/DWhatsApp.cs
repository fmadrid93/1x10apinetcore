using System;
using System.Data;
using Infraestructure;
using Microsoft.Data.SqlClient;

namespace Infrastructure
{
    public class DWhatsApp : DbHelper
    {
        /// <summary>
        /// Obtiene todas las personas movilizadas correspondientes al nodo del usuario según su rol:
        /// - MOVILIZADOR: Solo las personas que él registró directamente.
        /// - GERENTE: Personas de todos los movilizadores que están bajo su supervisión.
        /// - ADMINISTRADOR: Todas las personas activas registradas en la campaña.
        /// </summary>
        public DataTable ObtenerPersonasPorNodo(int idUsuario, string rol)
        {
            string rolUpper = (rol ?? string.Empty).Trim().ToUpper();

            if (rolUpper == "MOVILIZADOR")
            {
                string sql = @"
                    SELECT 
                        pm.IdPersonaMovilizada,
                        pm.IdUsuarioMovilizador,
                        pm.Nombres,
                        pm.Apellidos,
                        pm.CI,
                        pm.Celular,
                        pm.RecintoVotacion,
                        pm.EstadoDiaD,
                        u.NombreCompleto AS NombreMovilizador
                    FROM PersonaMovilizada pm WITH (NOLOCK)
                    LEFT JOIN Usuario u WITH (NOLOCK) ON pm.IdUsuarioMovilizador = u.IdUsuario
                    WHERE pm.IdUsuarioMovilizador = @IdUsuario 
                      AND (pm.Activo IS NULL OR pm.Activo = 1)
                      AND pm.Celular IS NOT NULL 
                      AND LTRIM(RTRIM(pm.Celular)) <> ''
                    ORDER BY pm.Apellidos, pm.Nombres";

                return EjecutarSQLDirecto(sql, new SqlParameter("@IdUsuario", SqlDbType.Int) { Value = idUsuario });
            }
            else if (rolUpper == "GERENTE")
            {
                string sql = @"
                    SELECT 
                        pm.IdPersonaMovilizada,
                        pm.IdUsuarioMovilizador,
                        pm.Nombres,
                        pm.Apellidos,
                        pm.CI,
                        pm.Celular,
                        pm.RecintoVotacion,
                        pm.EstadoDiaD,
                        u.NombreCompleto AS NombreMovilizador
                    FROM PersonaMovilizada pm WITH (NOLOCK)
                    INNER JOIN Usuario u WITH (NOLOCK) ON pm.IdUsuarioMovilizador = u.IdUsuario
                    WHERE (u.IdUsuarioSupervisor = @IdUsuario OR pm.IdUsuarioMovilizador = @IdUsuario)
                      AND (pm.Activo IS NULL OR pm.Activo = 1)
                      AND pm.Celular IS NOT NULL 
                      AND LTRIM(RTRIM(pm.Celular)) <> ''
                    ORDER BY pm.Apellidos, pm.Nombres";

                return EjecutarSQLDirecto(sql, new SqlParameter("@IdUsuario", SqlDbType.Int) { Value = idUsuario });
            }
            else
            {
                // ADMINISTRADOR u otros roles directivos (todo el ámbito)
                string sql = @"
                    SELECT 
                        pm.IdPersonaMovilizada,
                        pm.IdUsuarioMovilizador,
                        pm.Nombres,
                        pm.Apellidos,
                        pm.CI,
                        pm.Celular,
                        pm.RecintoVotacion,
                        pm.EstadoDiaD,
                        u.NombreCompleto AS NombreMovilizador
                    FROM PersonaMovilizada pm WITH (NOLOCK)
                    LEFT JOIN Usuario u WITH (NOLOCK) ON pm.IdUsuarioMovilizador = u.IdUsuario
                    WHERE (pm.Activo IS NULL OR pm.Activo = 1)
                      AND pm.Celular IS NOT NULL 
                      AND LTRIM(RTRIM(pm.Celular)) <> ''
                    ORDER BY pm.Apellidos, pm.Nombres";

                return EjecutarSQLDirecto(sql);
            }
        }

        /// <summary>
        /// Obtiene la ruta/URL del servidor de WhatsApp configurada para el territorio o municipio del usuario.
        /// Si el territorio del usuario no tiene URL propia, busca recursivamente en el territorio padre (ej. Municipio).
        /// </summary>
        public string? ObtenerUrlServidorWhatsAppPorUsuario(int idUsuario)
        {
            string sql = @"
                WITH ArbolTerritorio AS (
                    SELECT 
                        t.IdTerritorio,
                        t.IdTerritorioPadre,
                        t.UrlServidorWhatsApp,
                        1 AS Nivel
                    FROM Usuario u WITH (NOLOCK)
                    LEFT JOIN Usuario sup WITH (NOLOCK) ON u.IdUsuarioSupervisor = sup.IdUsuario
                    INNER JOIN Territorio t WITH (NOLOCK) ON (
                        u.IdTerritorio = t.IdTerritorio 
                        OR (u.IdTerritorio IS NULL AND sup.IdTerritorio = t.IdTerritorio)
                    )
                    WHERE u.IdUsuario = @IdUsuario

                    UNION ALL

                    SELECT 
                        tp.IdTerritorio,
                        tp.IdTerritorioPadre,
                        tp.UrlServidorWhatsApp,
                        a.Nivel + 1
                    FROM Territorio tp WITH (NOLOCK)
                    INNER JOIN ArbolTerritorio a ON tp.IdTerritorio = a.IdTerritorioPadre
                )
                SELECT TOP 1 UrlServidorWhatsApp 
                FROM ArbolTerritorio 
                WHERE UrlServidorWhatsApp IS NOT NULL 
                  AND LTRIM(RTRIM(UrlServidorWhatsApp)) <> ''
                ORDER BY Nivel ASC";

            try
            {
                DataTable dt = EjecutarSQLDirecto(sql, new SqlParameter("@IdUsuario", SqlDbType.Int) { Value = idUsuario });
                if (dt != null && dt.Rows.Count > 0)
                {
                    var val = dt.Rows[0]["UrlServidorWhatsApp"]?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(val)) return val;
                }
            }
            catch
            {
                // Si la columna aún no está creada o la consulta falla, retorna null para usar la URL por defecto
            }

            return null;
        }

        public DataTable ObtenerUsuariosParaMonitoreoWhatsApp(int idUsuarioSolicitante)
        {
            string sql = @"
                WITH ArbolJerarquia AS (
                    SELECT 
                        u.IdUsuario,
                        u.IdUsuarioSupervisor,
                        u.IdTerritorio,
                        u.IdRol
                    FROM Usuario u WITH (NOLOCK)
                    WHERE u.IdUsuario = @IdUsuarioSolicitante

                    UNION ALL

                    SELECT 
                        h.IdUsuario,
                        h.IdUsuarioSupervisor,
                        h.IdTerritorio,
                        h.IdRol
                    FROM Usuario h WITH (NOLOCK)
                    INNER JOIN ArbolJerarquia a ON h.IdUsuarioSupervisor = a.IdUsuario
                ),
                TerritoriosValidos AS (
                    SELECT 
                        t.IdTerritorio,
                        t.IdTerritorioPadre
                    FROM Usuario u WITH (NOLOCK)
                    INNER JOIN Territorio t WITH (NOLOCK) ON u.IdTerritorio = t.IdTerritorio
                    WHERE u.IdUsuario = @IdUsuarioSolicitante

                    UNION ALL

                    SELECT 
                        h.IdTerritorio,
                        h.IdTerritorioPadre
                    FROM Territorio h WITH (NOLOCK)
                    INNER JOIN TerritoriosValidos tv ON h.IdTerritorioPadre = tv.IdTerritorio
                )
                SELECT DISTINCT
                    u.IdUsuario,
                    u.NombreCompleto,
                    u.Usuario,
                    r.Nombre AS Rol,
                    u.IdTerritorio,
                    ISNULL(t.Nombre, 'Sin Territorio Asignado') AS NombreTerritorio,
                    t.TipoTerritorio,
                    t.UrlServidorWhatsApp
                FROM Usuario u WITH (NOLOCK)
                INNER JOIN Rol r WITH (NOLOCK) ON u.IdRol = r.IdRol
                LEFT JOIN Territorio t WITH (NOLOCK) ON u.IdTerritorio = t.IdTerritorio
                WHERE u.Activo = 1
                  AND (
                    -- Super Admin / Admin General (Si su IdTerritorio es NULL y es ADMINISTRADOR)
                    EXISTS (
                        SELECT 1 FROM Usuario sol WITH (NOLOCK) 
                        INNER JOIN Rol rsol WITH (NOLOCK) ON sol.IdRol = rsol.IdRol
                        WHERE sol.IdUsuario = @IdUsuarioSolicitante 
                          AND rsol.Nombre = 'ADMINISTRADOR'
                          AND sol.IdTerritorio IS NULL
                    )
                    -- O el usuario solicitante es 0 (bypass)
                    OR @IdUsuarioSolicitante = 0
                    -- O está en el árbol de supervisión del usuario
                    OR u.IdUsuario IN (SELECT IdUsuario FROM ArbolJerarquia)
                    -- O está en su territorio o en cualquier territorio hijo
                    OR u.IdTerritorio IN (SELECT IdTerritorio FROM TerritoriosValidos)
                  )
                ORDER BY NombreTerritorio, r.Nombre, u.NombreCompleto";

            return EjecutarSQLDirecto(sql, new SqlParameter("@IdUsuarioSolicitante", SqlDbType.Int) { Value = idUsuarioSolicitante });
        }

        private DataTable EjecutarSQLDirecto(string sql, params SqlParameter[] parameters)
        {
            DataSet ds = new DataSet();
            try
            {
                abrirConexion();
                using (SqlCommand cmd = new SqlCommand(sql, obtenerConexion()))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandTimeout = 120;
                    if (parameters != null && parameters.Length > 0)
                    {
                        cmd.Parameters.AddRange(parameters);
                    }
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(ds);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al consultar datos de WhatsApp: {ex.Message}");
            }
            finally
            {
                cerrarConexion();
            }

            return ds.Tables.Count > 0 ? ds.Tables[0] : new DataTable();
        }
    }
}
