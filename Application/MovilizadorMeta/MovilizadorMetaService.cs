using System.Data;
using Infrastructure;

namespace Application.MovilizadorMeta
{
    public class MovilizadorMetaService
    {
        private readonly DMovilizadorMeta _data = new DMovilizadorMeta();

        public DataTable Listar()
        {
            return _data.Listar();
        }

        public DataTable Listar(int idUsuarioCaller, int? idTerritorioCaller, string rolCaller)
        {
            return _data.ListarPorEstructura(idUsuarioCaller, idTerritorioCaller, rolCaller);
        }

        public DataTable Obtener(int idUsuarioMovilizador)
        {
            return _data.Obtener(idUsuarioMovilizador);
        }

        public DataTable Guardar(int idUsuarioMovilizador, int metaObjetivo)
        {
            return _data.Guardar(idUsuarioMovilizador, metaObjetivo);
        }

        public DataTable ListarPorGerente(int idGerente)
        {
            return _data.ListarPorGerente(idGerente);
        }
    }
}