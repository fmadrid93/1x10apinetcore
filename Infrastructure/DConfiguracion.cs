using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace Infrastructure
{
    public class DConfiguracion : DbHelper
    {
        private static bool _garantizarTablaEjecutada = false;

        private void GarantizarTabla()
        {
            if (_garantizarTablaEjecutada) return;

            try
            {
                string sql = @"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ConfiguracionGeneral')
                    BEGIN
                        CREATE TABLE ConfiguracionGeneral (
                            Clave VARCHAR(100) PRIMARY KEY,
                            Valor VARCHAR(500) NOT NULL,
                            Descripcion VARCHAR(250) NULL,
                            FechaModificacion DATETIME DEFAULT GETDATE()
                        );
                        INSERT INTO ConfiguracionGeneral (Clave, Valor, Descripcion)
                        VALUES ('PERMITIR_VOTANTES_DUPLICADOS', '0', 'Permite o bloquea el registro de votantes/personas con CI duplicado (0=Bloquear, 1=Permitir)');
                    END";
                EjecutarSQL(sql);
                _garantizarTablaEjecutada = true;
            }
            catch
            {
                // Si falla por permisos, continuará con los valores por defecto
            }
        }

        public string ObtenerValor(string clave, string valorPorDefecto = "")
        {
            GarantizarTabla();
            try
            {
                string sql = "SELECT TOP 1 Valor FROM ConfiguracionGeneral WITH (NOLOCK) WHERE Clave = @Clave";
                var dt = EjecutarSQL(sql, new SqlParameter("@Clave", SqlDbType.VarChar, 100) { Value = clave });
                if (dt != null && dt.Rows.Count > 0 && dt.Rows[0]["Valor"] != DBNull.Value)
                {
                    return dt.Rows[0]["Valor"].ToString() ?? valorPorDefecto;
                }
            }
            catch
            {
                // Retorna valor por defecto
            }
            return valorPorDefecto;
        }

        public string ObtenerValorConHerencia(string prefijoClave, int? idTerritorio, int? idUsuario = null, string valorPorDefecto = "")
        {
            GarantizarTabla();
            try
            {
                string sql = @"
                    ;WITH Jerarquia AS (
                        SELECT u.IdUsuario, u.IdUsuarioSupervisor, u.IdTerritorio, 1 AS Nivel
                        FROM Usuario u WITH (NOLOCK)
                        WHERE u.IdUsuario = @IdUsuario AND @IdUsuario IS NOT NULL
                        UNION ALL
                        SELECT sup.IdUsuario, sup.IdUsuarioSupervisor, sup.IdTerritorio, j.Nivel + 1
                        FROM Usuario sup WITH (NOLOCK)
                        INNER JOIN Jerarquia j ON sup.IdUsuario = j.IdUsuarioSupervisor
                    ),
                    TerritoriosJerarquia AS (
                        SELECT IdTerritorio, Nivel FROM Jerarquia WHERE IdTerritorio IS NOT NULL
                        UNION
                        SELECT @IdTerritorio, 0 WHERE @IdTerritorio IS NOT NULL
                    ),
                    ArbolPadres AS (
                        SELECT tj.IdTerritorio, t.IdTerritorioPadre, tj.Nivel
                        FROM TerritoriosJerarquia tj
                        INNER JOIN Territorio t WITH (NOLOCK) ON t.IdTerritorio = tj.IdTerritorio
                        UNION ALL
                        SELECT tp.IdTerritorio, tp.IdTerritorioPadre, a.Nivel + 1
                        FROM Territorio tp WITH (NOLOCK)
                        INNER JOIN ArbolPadres a ON tp.IdTerritorio = a.IdTerritorioPadre
                    )
                    SELECT TOP 1 cg.Valor
                    FROM ArbolPadres a
                    INNER JOIN ConfiguracionGeneral cg WITH (NOLOCK) ON cg.Clave = @PrefijoClave + '_' + CAST(a.IdTerritorio AS VARCHAR)
                    ORDER BY a.Nivel ASC";

                var parametros = new System.Collections.Generic.List<SqlParameter>
                {
                    new SqlParameter("@PrefijoClave", SqlDbType.VarChar, 100) { Value = prefijoClave },
                    new SqlParameter("@IdTerritorio", SqlDbType.Int) { Value = (object?)idTerritorio ?? DBNull.Value },
                    new SqlParameter("@IdUsuario", SqlDbType.Int) { Value = (object?)idUsuario ?? DBNull.Value }
                };

                var dt = EjecutarSQL(sql, parametros.ToArray());
                if (dt != null && dt.Rows.Count > 0 && dt.Rows[0]["Valor"] != DBNull.Value)
                {
                    return dt.Rows[0]["Valor"].ToString() ?? valorPorDefecto;
                }
            }
            catch { }

            // Fallback a la clave global base
            return ObtenerValor(prefijoClave, valorPorDefecto);
        }

        public bool GuardarValor(string clave, string valor, string? descripcion = null)
        {
            GarantizarTabla();
            try
            {
                string sql = @"
                    IF EXISTS (SELECT 1 FROM ConfiguracionGeneral WHERE Clave = @Clave)
                    BEGIN
                        UPDATE ConfiguracionGeneral 
                        SET Valor = @Valor, 
                            Descripcion = ISNULL(@Descripcion, Descripcion),
                            FechaModificacion = GETDATE()
                        WHERE Clave = @Clave;
                    END
                    ELSE
                    BEGIN
                        INSERT INTO ConfiguracionGeneral (Clave, Valor, Descripcion, FechaModificacion)
                        VALUES (@Clave, @Valor, @Descripcion, GETDATE());
                    END";

                EjecutarSQL(sql,
                    new SqlParameter("@Clave", SqlDbType.VarChar, 100) { Value = clave },
                    new SqlParameter("@Valor", SqlDbType.VarChar, 500) { Value = valor },
                    new SqlParameter("@Descripcion", SqlDbType.VarChar, 250) { Value = (object?)descripcion ?? DBNull.Value }
                );
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool ObtenerPermitirDuplicados()
        {
            // Por defecto: false (0 = no permitir)
            string valor = ObtenerValor("PERMITIR_VOTANTES_DUPLICADOS", "0");
            return valor == "1" || valor.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        public bool GuardarPermitirDuplicados(bool permitir)
        {
            return GuardarValor(
                "PERMITIR_VOTANTES_DUPLICADOS",
                permitir ? "1" : "0",
                "Permite o bloquea el registro de votantes/personas con CI duplicado (0=Bloquear, 1=Permitir)"
            );
        }
    }
}
