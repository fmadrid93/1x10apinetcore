using System.Data;
using Infrastructure;

namespace Application.DiaD
{
    public class DiaDService
    {
        private readonly DDiaD _data = new DDiaD();

        public DataTable MarcarEstado(int idPersonaMovilizada, int idUsuarioMarca, string estadoDiaD, string? observacion)
        {
            return _data.MarcarEstado(idPersonaMovilizada, idUsuarioMarca, estadoDiaD, observacion);
        }

        public DataTable HistorialPorPersona(int idPersonaMovilizada)
        {
            return _data.HistorialPorPersona(idPersonaMovilizada);
        }

        public DataTable ListarPorMovilizador(int idUsuarioMovilizador)
        {
            return _data.ListarPorMovilizador(idUsuarioMovilizador);
        }

        public DataTable ResumenAdmin()
        {
            return _data.ResumenAdmin();
        }

        public DataTable ResumenGerente(int idGerente)
        {
            return _data.ResumenGerente(idGerente);
        }

        public DataTable VelocidadVotacion30Min()
        {
            return _data.VelocidadVotacion30Min();
        }

        public DataTable CurvaAvance()
        {
            return _data.CurvaAvance();
        }

        public DataTable BrechaMetaHora()
        {
            return _data.BrechaMetaHora();
        }
    }
}