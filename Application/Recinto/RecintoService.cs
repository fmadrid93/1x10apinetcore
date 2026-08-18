using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using Infrastructure;

namespace Application.Recinto
{
  

    public class RecintoService
    {
        private readonly DRecinto _data = new DRecinto();

        public DataTable Listar()
        {
            return _data.Listar();
        }
    }
}
