using System.Data;
using Infrastructure;

namespace Application.Territorio
{
    public class TerritorioService
    {
        private readonly DTerritorio _data = new DTerritorio();

        public DataTable Insertar(int? idTerritorioPadre, string nombre, string tipoTerritorio, string? codigo, string? urlServidorWhatsApp = null)
        {
            return _data.Insertar(idTerritorioPadre, nombre, tipoTerritorio, codigo, urlServidorWhatsApp);
        }

        public DataTable Listar(bool soloActivos)
        {
            return _data.Listar(soloActivos);
        }

        public DataTable ObtenerPorId(int idTerritorio)
        {
            return _data.ObtenerPorId(idTerritorio);
        }

        public DataTable Actualizar(int idTerritorio, int? idTerritorioPadre, string nombre, string tipoTerritorio, string? codigo, bool activo, string? urlServidorWhatsApp = null)
        {
            return _data.Actualizar(idTerritorio, idTerritorioPadre, nombre, tipoTerritorio, codigo, activo, urlServidorWhatsApp);
        }
    }
}