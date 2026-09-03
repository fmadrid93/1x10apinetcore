using System;
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

        public DataTable ListarPorEstructura(int idUsuarioCaller, int? idTerritorioCaller, string rolCaller)
        {
            string rolUpper = (rolCaller ?? string.Empty).Trim().ToUpper();

            if (rolUpper == "GERENTE")
            {
                string sql = @"
                    SELECT 
                        ISNULL(mm.IdMovilizadorMeta, 0) AS IdMovilizadorMeta,
                        u.IdUsuario AS IdUsuarioMovilizador,
                        u.NombreCompleto AS Movilizador,
                        ISNULL(mm.MetaObjetivo, 10) AS MetaObjetivo,
                        mm.FechaCreate,
                        mm.FechaUpdate,
                        ISNULL((SELECT COUNT(1) FROM PersonaMovilizada pm WITH (NOLOCK) WHERE pm.IdUsuarioMovilizador = u.IdUsuario AND (pm.Activo IS NULL OR pm.Activo = 1)), 0) AS TotalPersonas
                    FROM Usuario u WITH (NOLOCK)
                    LEFT JOIN MovilizadorMeta mm WITH (NOLOCK) ON u.IdUsuario = mm.IdUsuarioMovilizador
                    WHERE u.IdRol = 3 -- MOVILIZADOR
                      AND (u.Activo IS NULL OR u.Activo = 1)
                      AND (u.IdUsuarioSupervisor = @IdUsuarioCaller OR u.IdUsuarioCreate = @IdUsuarioCaller)
                    ORDER BY u.NombreCompleto";

                return EjecutarSQL(sql, new SqlParameter("@IdUsuarioCaller", SqlDbType.Int) { Value = idUsuarioCaller });
            }
            else if (idTerritorioCaller.HasValue)
            {
                // Admin territorial: Movilizadores bajo su árbol de territorios o supervisados/creados por sus gerentes o por él
                string sql = @"
                    WITH ArbolTerritorios AS (
                        SELECT IdTerritorio FROM Territorio WITH (NOLOCK) WHERE IdTerritorio = @IdTerritorioCaller
                        UNION ALL
                        SELECT t.IdTerritorio FROM Territorio t WITH (NOLOCK)
                        INNER JOIN ArbolTerritorios a ON t.IdTerritorioPadre = a.IdTerritorio
                    ),
                    GerentesDelAdmin AS (
                        SELECT IdUsuario FROM Usuario WITH (NOLOCK)
                        WHERE IdRol = 2 AND (IdTerritorio IN (SELECT IdTerritorio FROM ArbolTerritorios) OR IdUsuarioSupervisor = @IdUsuarioCaller OR IdUsuarioCreate = @IdUsuarioCaller)
                    )
                    SELECT 
                        ISNULL(mm.IdMovilizadorMeta, 0) AS IdMovilizadorMeta,
                        u.IdUsuario AS IdUsuarioMovilizador,
                        u.NombreCompleto AS Movilizador,
                        ISNULL(mm.MetaObjetivo, 10) AS MetaObjetivo,
                     --   mm.FechaCreate,
                      --  mm.FechaUpdate,
                        ISNULL((SELECT COUNT(1) FROM PersonaMovilizada pm WITH (NOLOCK) WHERE pm.IdUsuarioMovilizador = u.IdUsuario AND (pm.Activo IS NULL OR pm.Activo = 1)), 0) AS TotalPersonas
                    FROM Usuario u WITH (NOLOCK)
                    LEFT JOIN MovilizadorMeta mm WITH (NOLOCK) ON u.IdUsuario = mm.IdUsuarioMovilizador
                    WHERE u.IdRol = 3 -- MOVILIZADOR
                      AND (u.Activo IS NULL OR u.Activo = 1)
                      AND (
                          u.IdTerritorio IN (SELECT IdTerritorio FROM ArbolTerritorios)
                          OR u.IdUsuarioSupervisor IN (SELECT IdUsuario FROM GerentesDelAdmin)
                          OR u.IdUsuarioSupervisor = @IdUsuarioCaller
                          OR u.IdUsuarioCreate = @IdUsuarioCaller
                      )
                    ORDER BY u.NombreCompleto";

                return EjecutarSQL(sql, 
                    new SqlParameter("@IdUsuarioCaller", SqlDbType.Int) { Value = idUsuarioCaller },
                    new SqlParameter("@IdTerritorioCaller", SqlDbType.Int) { Value = idTerritorioCaller.Value });
            }
            else
            {
                // Super Admin: Listar todos los movilizadores activos
                string sql = @"
                    SELECT 
                        ISNULL(mm.IdMovilizadorMeta, 0) AS IdMovilizadorMeta,
                        u.IdUsuario AS IdUsuarioMovilizador,
                        u.NombreCompleto AS Movilizador,
                        ISNULL(mm.MetaObjetivo, 10) AS MetaObjetivo,
                        mm.FechaCreate,
                        mm.FechaUpdate,
                        ISNULL((SELECT COUNT(1) FROM PersonaMovilizada pm WITH (NOLOCK) WHERE pm.IdUsuarioMovilizador = u.IdUsuario AND (pm.Activo IS NULL OR pm.Activo = 1)), 0) AS TotalPersonas
                    FROM Usuario u WITH (NOLOCK)
                    LEFT JOIN MovilizadorMeta mm WITH (NOLOCK) ON u.IdUsuario = mm.IdUsuarioMovilizador
                    WHERE u.IdRol = 3 -- MOVILIZADOR
                      AND (u.Activo IS NULL OR u.Activo = 1)
                    ORDER BY u.NombreCompleto";

                return EjecutarSQL(sql);
            }
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
            string sql = @"
                SELECT 
                    ISNULL(mm.IdMovilizadorMeta, 0) AS IdMovilizadorMeta,
                    u.IdUsuario AS IdUsuarioMovilizador,
                    u.NombreCompleto AS Movilizador,
                    ISNULL(mm.MetaObjetivo, 10) AS MetaObjetivo,
                    mm.FechaCreate,
                    mm.FechaUpdate,
                    ISNULL((SELECT COUNT(1) FROM PersonaMovilizada pm WITH (NOLOCK) WHERE pm.IdUsuarioMovilizador = u.IdUsuario AND (pm.Activo IS NULL OR pm.Activo = 1)), 0) AS TotalPersonas
                FROM Usuario u WITH (NOLOCK)
                LEFT JOIN MovilizadorMeta mm WITH (NOLOCK) ON u.IdUsuario = mm.IdUsuarioMovilizador
                WHERE u.IdRol = 3 -- MOVILIZADOR
                  AND (u.Activo IS NULL OR u.Activo = 1)
                  AND (u.IdUsuarioSupervisor = @IdGerente OR u.IdUsuarioCreate = @IdGerente)
                ORDER BY u.NombreCompleto";

            return EjecutarSQL(sql, new SqlParameter("@IdGerente", SqlDbType.Int) { Value = idGerente });
        }

        public (int metaObjetivo, int totalRegistrados) ObtenerMetaYTotalPersonas(int idUsuarioMovilizador)
        {

            try
            {
                string sql = @"
                    SELECT 
                        ISNULL((SELECT TOP 1 MetaObjetivo FROM MovilizadorMeta WITH (NOLOCK) WHERE IdUsuarioMovilizador = @IdUsuarioMovilizador), 10) AS MetaObjetivo,
                        ISNULL((SELECT COUNT(1) FROM PersonaMovilizada WITH (NOLOCK) WHERE IdUsuarioMovilizador = @IdUsuarioMovilizador AND (Activo IS NULL OR Activo = 1)), 0) AS TotalPersonas";
                var dt = EjecutarSQL(sql, new SqlParameter("@IdUsuarioMovilizador", SqlDbType.Int) { Value = idUsuarioMovilizador });
                if (dt != null && dt.Rows.Count > 0)
                {
                    int meta = Convert.ToInt32(dt.Rows[0]["MetaObjetivo"]);
                    int total = Convert.ToInt32(dt.Rows[0]["TotalPersonas"]);
                    return (meta, total);
                }
            }
            catch { }
            return (10, 0);
        }
    }
}