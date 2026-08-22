using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Infrastructure
{


    public class DRecinto : DbHelper
    {
        public DataTable ListarXTerritorio(int idTerritorio)
        {
            return EjecutarPA("PA_RECINTO_LISTAR_X_MUNICIPIO",
                new SqlParameter("@IdMunicipio", SqlDbType.Int) { Value = (object?)idTerritorio ?? DBNull.Value });
        }
        public DataTable Listar()
        {
            return EjecutarPA("PA_RECINTO_LISTAR");
        }
    }
}
