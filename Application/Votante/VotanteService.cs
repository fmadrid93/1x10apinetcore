using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace Application.Votante
{
  

    public class VotanteService
    {
        private readonly DVotante _data = new DVotante();
        public DataTable ObtenerVotante(string ci)
        {
            return _data.ObtenerVotante(ci);
        }
        public DataTable BuscarPadronGlobal(string texto, string? idRecinto = null, string? nroMesa = null)
        {
            return _data.BuscarPadronGlobal(texto, idRecinto, nroMesa);
        }

        public DataTable MarcarYaVoto(int idVotante, int idUsuarioMarca, string? observacion)
        {
            return _data.MarcarYaVoto(idVotante, idUsuarioMarca, observacion);
        }

        public DataTable ObtenerTop10(int? idTerritorio = null)
        {
            return _data.ObtenerTop10(idTerritorio);
        }

        public DataTable ObtenerTop50(int? idTerritorio = null)
        {
            return _data.ObtenerTop10(idTerritorio);
        }
    }
}
