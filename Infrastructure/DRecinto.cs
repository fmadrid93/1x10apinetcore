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
        public DataTable Listar()
        {
            return EjecutarPA("PA_RECINTO_LISTAR");
        }
    }
}
