using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace Infrastructure
{
    public class DPersonaMovilizada : DbHelper
    {



            public DataTable Insertar(
                int idUsuarioMovilizador,
                int? idTerritorio,
                string nombres,
                string apellidos,
                string? ci,
                string? celular,
                string? direccionReferencia,
                string? sexo,
                string? rangoEdad,
                string? recintoVotacion,
                string? idRecinto,
                bool? requiereAyudaVotar,
                string? nivelCompromiso,
                string? observaciones,
                decimal? latitud,
                decimal? longitud
            )
            {
                return EjecutarPA(
                    "pa_persona_movilizada_insertar",
                    new SqlParameter("@IdUsuarioMovilizador", SqlDbType.Int) { Value = idUsuarioMovilizador },
                    new SqlParameter("@IdTerritorio", SqlDbType.Int) { Value = (object?)idTerritorio ?? DBNull.Value },
                    new SqlParameter("@Nombres", SqlDbType.VarChar, 150) { Value = nombres },
                    new SqlParameter("@Apellidos", SqlDbType.VarChar, 150) { Value = apellidos },
                    new SqlParameter("@CI", SqlDbType.VarChar, 30) { Value = (object?)ci ?? DBNull.Value },
                    new SqlParameter("@Celular", SqlDbType.VarChar, 30) { Value = (object?)celular ?? DBNull.Value },
                    new SqlParameter("@DireccionReferencia", SqlDbType.VarChar, 250) { Value = (object?)direccionReferencia ?? DBNull.Value },
                    new SqlParameter("@Sexo", SqlDbType.VarChar, 20) { Value = (object?)sexo ?? DBNull.Value },
                    new SqlParameter("@RangoEdad", SqlDbType.VarChar, 30) { Value = (object?)rangoEdad ?? DBNull.Value },
                    new SqlParameter("@RecintoVotacion", SqlDbType.VarChar, 150) { Value = (object?)recintoVotacion ?? DBNull.Value },
                     new SqlParameter("@IdRecinto", SqlDbType.VarChar, 150) { Value = (object?)idRecinto ?? DBNull.Value },
                    new SqlParameter("@RequiereAyudaVotar", SqlDbType.Bit) { Value = (object?)requiereAyudaVotar ?? DBNull.Value },
                    new SqlParameter("@NivelCompromiso", SqlDbType.VarChar, 30) { Value = (object?)nivelCompromiso ?? DBNull.Value },
                    new SqlParameter("@Observaciones", SqlDbType.VarChar, 500) { Value = (object?)observaciones ?? DBNull.Value },
                    new SqlParameter("@Latitud", SqlDbType.Decimal) { Value = (object?)latitud ?? DBNull.Value, Precision = 10, Scale = 8 },
                    new SqlParameter("@Longitud", SqlDbType.Decimal) { Value = (object?)longitud ?? DBNull.Value, Precision = 11, Scale = 8 }
                );
            }

            public DataTable Actualizar(
                int idPersonaMovilizada,
                int idUsuarioMovilizador,
                int? idTerritorio,
                string nombres,
                string apellidos,
                string? ci,
                string? celular,
                string? direccionReferencia,
                string? sexo,
                string? rangoEdad,
                string? recintoVotacion,
                  string? idRecinto,
                bool? requiereAyudaVotar,
                string? nivelCompromiso,
                string? observaciones,
                decimal? latitud,
                decimal? longitud
            )
            {
                return EjecutarPA(
                    "pa_persona_movilizada_actualizar",
                    new SqlParameter("@IdPersonaMovilizada", SqlDbType.Int) { Value = idPersonaMovilizada },
                    new SqlParameter("@IdUsuarioMovilizador", SqlDbType.Int) { Value = idUsuarioMovilizador },
                    new SqlParameter("@IdTerritorio", SqlDbType.Int) { Value = (object?)idTerritorio ?? DBNull.Value },
                    new SqlParameter("@Nombres", SqlDbType.VarChar, 150) { Value = nombres },
                    new SqlParameter("@Apellidos", SqlDbType.VarChar, 150) { Value = apellidos },
                    new SqlParameter("@CI", SqlDbType.VarChar, 30) { Value = (object?)ci ?? DBNull.Value },
                    new SqlParameter("@Celular", SqlDbType.VarChar, 30) { Value = (object?)celular ?? DBNull.Value },
                    new SqlParameter("@DireccionReferencia", SqlDbType.VarChar, 250) { Value = (object?)direccionReferencia ?? DBNull.Value },
                    new SqlParameter("@Sexo", SqlDbType.VarChar, 20) { Value = (object?)sexo ?? DBNull.Value },
                    new SqlParameter("@RangoEdad", SqlDbType.VarChar, 30) { Value = (object?)rangoEdad ?? DBNull.Value },
                    new SqlParameter("@RecintoVotacion", SqlDbType.VarChar, 150) { Value = (object?)recintoVotacion ?? DBNull.Value },

                     new SqlParameter("@IdRecinto", SqlDbType.VarChar, 150) { Value = (object?)idRecinto ?? DBNull.Value },
                    new SqlParameter("@RequiereAyudaVotar", SqlDbType.Bit) { Value = (object?)requiereAyudaVotar ?? DBNull.Value },
                    new SqlParameter("@NivelCompromiso", SqlDbType.VarChar, 30) { Value = (object?)nivelCompromiso ?? DBNull.Value },
                    new SqlParameter("@Observaciones", SqlDbType.VarChar, 500) { Value = (object?)observaciones ?? DBNull.Value },
                    new SqlParameter("@Latitud", SqlDbType.Decimal) { Value = (object?)latitud ?? DBNull.Value, Precision = 10, Scale = 8 },
                    new SqlParameter("@Longitud", SqlDbType.Decimal) { Value = (object?)longitud ?? DBNull.Value, Precision = 11, Scale = 8 }
                );
            }



        public DataTable EliminarLogico(int idPersonaMovilizada, int idUsuarioMovilizador)
        {
            return EjecutarPA(
                "pa_persona_movilizada_eliminar_logico",
                new SqlParameter("@IdPersonaMovilizada", SqlDbType.Int) { Value = idPersonaMovilizada },
                new SqlParameter("@IdUsuarioMovilizador", SqlDbType.Int) { Value = idUsuarioMovilizador }
            );
        }

        public DataTable ObtenerPorId(int idPersonaMovilizada)
        {
            return EjecutarPA(
                "pa_persona_movilizada_obtener_por_id",
                new SqlParameter("@IdPersonaMovilizada", SqlDbType.Int) { Value = idPersonaMovilizada }
            );
        }

        public DataTable ListarPorMovilizador(int idUsuarioMovilizador, string? texto, string? estadoDiaD)
        {
            return EjecutarPA(
                "pa_persona_movilizada_listar_por_movilizador",
                new SqlParameter("@IdUsuarioMovilizador", SqlDbType.Int) { Value = idUsuarioMovilizador },
                new SqlParameter("@Texto", SqlDbType.VarChar, 150) { Value = (object?)texto ?? DBNull.Value },
                new SqlParameter("@EstadoDiaD", SqlDbType.VarChar, 30) { Value = (object?)estadoDiaD ?? DBNull.Value }
            );
        }

        public DataTable BuscarGeneral(int? idTerritorio, int? idUsuarioMovilizador, string? texto, string? estadoDiaD)
        {
            return EjecutarPA(
                "pa_persona_movilizada_buscar_general",
                new SqlParameter("@IdTerritorio", SqlDbType.Int) { Value = (object?)idTerritorio ?? DBNull.Value },
                new SqlParameter("@IdUsuarioMovilizador", SqlDbType.Int) { Value = (object?)idUsuarioMovilizador ?? DBNull.Value },
                new SqlParameter("@Texto", SqlDbType.VarChar, 150) { Value = (object?)texto ?? DBNull.Value },
                new SqlParameter("@EstadoDiaD", SqlDbType.VarChar, 30) { Value = (object?)estadoDiaD ?? DBNull.Value }
            );
        }

        public DataTable ResumenMovilizador(int idUsuarioMovilizador)
        {
            return EjecutarPA(
                "pa_persona_movilizada_resumen_movilizador",
                new SqlParameter("@IdUsuarioMovilizador", SqlDbType.Int) { Value = idUsuarioMovilizador }
            );
        }

        public DataTable VerificarExisteCI(string ci, int? excludeIdPersona = null)
        {
            string sql = @"
                SELECT TOP 1
                    pm.IdPersonaMovilizada,
                    (ISNULL(pm.Nombres, '') + ' ' + ISNULL(pm.Apellidos, '')) AS NombreCompleto,
                    ISNULL(u.NombreCompleto, 'Desconocido') AS NombreMovilizador
                FROM PersonaMovilizada pm WITH (NOLOCK)
                LEFT JOIN Usuario u WITH (NOLOCK) ON pm.IdUsuarioMovilizador = u.IdUsuario
                WHERE (pm.Activo IS NULL OR pm.Activo = 1)
                  AND LTRIM(RTRIM(pm.CI)) = @CI";

            if (excludeIdPersona.HasValue && excludeIdPersona.Value > 0)
            {
                sql += " AND pm.IdPersonaMovilizada <> @ExcludeId";
                return EjecutarSQL(sql,
                    new SqlParameter("@CI", SqlDbType.VarChar, 50) { Value = ci.Trim() },
                    new SqlParameter("@ExcludeId", SqlDbType.Int) { Value = excludeIdPersona.Value }
                );
            }

            return EjecutarSQL(sql, new SqlParameter("@CI", SqlDbType.VarChar, 50) { Value = ci.Trim() });
        }

        /// <summary>
        /// Cuenta cuántas PersonaMovilizada activas ya tienen este CI, y de esas
        /// cuántas caen bajo el mismo movilizador (eso último NUNCA se permite,
        /// tenga o no habilitados los duplicados a nivel territorio).
        /// </summary>
        public (int Total, int EnMismoMovilizador) ContarPorCI(string ci, int idUsuarioMovilizador, int? excludeIdPersona = null)
        {
            string sql = @"
                SELECT
                    COUNT(*) AS Total,
                    SUM(CASE WHEN IdUsuarioMovilizador = @IdUsuarioMovilizador THEN 1 ELSE 0 END) AS EnMismoMovilizador
                FROM PersonaMovilizada WITH (NOLOCK)
                WHERE (Activo IS NULL OR Activo = 1)
                  AND LTRIM(RTRIM(CI)) = @CI";

            var parametros = new List<SqlParameter>
            {
                new SqlParameter("@CI", SqlDbType.VarChar, 50) { Value = ci.Trim() },
                new SqlParameter("@IdUsuarioMovilizador", SqlDbType.Int) { Value = idUsuarioMovilizador },
            };

            if (excludeIdPersona.HasValue && excludeIdPersona.Value > 0)
            {
                sql += " AND IdPersonaMovilizada <> @ExcludeId";
                parametros.Add(new SqlParameter("@ExcludeId", SqlDbType.Int) { Value = excludeIdPersona.Value });
            }

            var dt = EjecutarSQL(sql, parametros.ToArray());
            if (dt != null && dt.Rows.Count > 0)
            {
                int total = dt.Rows[0]["Total"] == DBNull.Value ? 0 : Convert.ToInt32(dt.Rows[0]["Total"]);
                int enMismo = dt.Rows[0]["EnMismoMovilizador"] == DBNull.Value ? 0 : Convert.ToInt32(dt.Rows[0]["EnMismoMovilizador"]);
                return (total, enMismo);
            }
            return (0, 0);
        }

        public System.Collections.Generic.HashSet<string> ListarTodosLosCIExistentes()
        {
            var hashSet = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                string sql = "SELECT LTRIM(RTRIM(CI)) AS CI FROM PersonaMovilizada WITH (NOLOCK) WHERE (Activo IS NULL OR Activo = 1) AND CI IS NOT NULL AND LTRIM(RTRIM(CI)) <> ''";
                var dt = EjecutarSQL(sql);
                if (dt != null && dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        string ci = row["CI"]?.ToString()?.Trim() ?? "";
                        if (!string.IsNullOrEmpty(ci))
                        {
                            hashSet.Add(ci);
                        }
                    }
                }
            }
            catch { }
            return hashSet;
        }

        /// <summary>
        /// Sincroniza el "Día D" de PersonaMovilizada cuando un Verificador marca a
        /// alguien como YA_VOTO desde el padrón oficial (TB_Votante): son dos tablas
        /// separadas y sin esto el dashboard (que lee de PersonaMovilizada) nunca se
        /// entera. Actualiza TODAS las filas activas con ese CI (por si hay
        /// duplicados del mismo movilizador).
        /// </summary>
        public int MarcarYaVotoPorCI(string ci)
        {
            string sql = @"
                UPDATE PersonaMovilizada
                SET EstadoDiaD = 'YA_VOTO'
                WHERE LTRIM(RTRIM(CI)) = @CI
                  AND (Activo IS NULL OR Activo = 1);
                SELECT @@ROWCOUNT AS Filas;";

            var dt = EjecutarSQL(sql, new SqlParameter("@CI", SqlDbType.VarChar, 50) { Value = ci.Trim() });
            if (dt != null && dt.Rows.Count > 0 && dt.Rows[0]["Filas"] != DBNull.Value)
            {
                return Convert.ToInt32(dt.Rows[0]["Filas"]);
            }
            return 0;
        }
    }
}