using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace Infrastructure
{
    public class DTerritorio : DbHelper
    {
        public DataTable Insertar(int? idTerritorioPadre, string nombre, string tipoTerritorio, string? codigo)
        {
            return EjecutarPA(
                "pa_territorio_insertar",
                new SqlParameter("@IdTerritorioPadre", SqlDbType.Int) { Value = (object?)idTerritorioPadre ?? DBNull.Value },
                new SqlParameter("@Nombre", SqlDbType.VarChar, 150) { Value = nombre },
                new SqlParameter("@TipoTerritorio", SqlDbType.VarChar, 50) { Value = tipoTerritorio },
                new SqlParameter("@Codigo", SqlDbType.VarChar, 50) { Value = (object?)codigo ?? DBNull.Value }
            );
        }

        public DataTable Listar(bool soloActivos)
        {
            return EjecutarPA(
                "pa_territorio_listar",
                new SqlParameter("@SoloActivos", SqlDbType.Bit) { Value = soloActivos }
            );
        }

        public DataTable ObtenerPorId(int idTerritorio)
        {
            return EjecutarPA(
                "pa_territorio_obtener_por_id",
                new SqlParameter("@IdTerritorio", SqlDbType.Int) { Value = idTerritorio }
            );
        }
        public DataTable Actualizar(int idTerritorio, int? idTerritorioPadre, string nombre, string tipoTerritorio, string? codigo, bool activo)
        {
            return EjecutarPA(
                "pa_territorio_actualizar",
                new SqlParameter("@IdTerritorio", SqlDbType.Int) { Value = idTerritorio },
                new SqlParameter("@IdTerritorioPadre", SqlDbType.Int) { Value = (object?)idTerritorioPadre ?? DBNull.Value },
                new SqlParameter("@Nombre", SqlDbType.VarChar, 150) { Value = nombre },
                new SqlParameter("@TipoTerritorio", SqlDbType.VarChar, 50) { Value = tipoTerritorio },
                new SqlParameter("@Codigo", SqlDbType.VarChar, 50) { Value = (object?)codigo ?? DBNull.Value },
                new SqlParameter("@Activo", SqlDbType.Bit) { Value = activo }
            );
        }
    }
}