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
        public DataTable BuscarPadronGlobal(string texto)
        {
            return _data.BuscarPadronGlobal(texto);
        }

        public DataTable MarcarYaVoto(int idVotante, int idUsuarioMarca, string? observacion)
        {
            return _data.MarcarYaVoto(idVotante, idUsuarioMarca, observacion);
        }
    }
}
