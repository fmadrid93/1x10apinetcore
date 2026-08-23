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
