using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using Infrastructure;

namespace Application.Votante
{


    public class VotanteService
    {
        private readonly DVotante _data = new DVotante();
        private readonly DPersonaMovilizada _dPersonaMovilizada = new DPersonaMovilizada();
        public DataTable ObtenerVotante(string ci)
        {
            return _data.ObtenerVotante(ci);
        }
        public DataTable BuscarPadronGlobal(string texto, string? idRecinto = null, string? nroMesa = null)
        {
            return _data.BuscarPadronGlobal(texto, idRecinto, nroMesa);
        }

        public DataTable MarcarYaVoto(string idVotante, int idUsuarioMarca, string? observacion)
        {
            var resultado = _data.MarcarYaVoto(idVotante, idUsuarioMarca, observacion);

            // Sincronizar con PersonaMovilizada para que el dashboard/Día D (que lee
            // de ahí, no de TB_Votante) refleje esta marca. Si el CI no está
            // registrado como PersonaMovilizada, simplemente no hay nada que
            // sincronizar (0 filas afectadas) y no es un error.
            try
            {
                string? ci = _data.ObtenerCIPorId(idVotante);
                if (!string.IsNullOrWhiteSpace(ci))
                {
                    _dPersonaMovilizada.MarcarYaVotoPorCI(ci);
                }
            }
            catch
            {
                // No se deja que un fallo de sincronización tumbe la marca en el
                // padrón oficial, que ya se guardó correctamente arriba.
            }

            return resultado;
        }

        public DataTable MarcarPasoPorElPC(string idVotante, int idUsuarioMarca)
        {
            int filas = _data.MarcarPasoPorElPC(idVotante, idUsuarioMarca);
            var dt = new DataTable();
            dt.Columns.Add("FilasAfectadas", typeof(int));
            dt.Rows.Add(filas);
            return dt;
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
