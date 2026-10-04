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

        public DataTable BuscarPadronGlobal(string texto, string? idRecinto = null, string? nroMesa = null, int? idTerritorio = null)
        {
            return _data.BuscarPadronGlobal(texto, idRecinto, nroMesa, idTerritorio);
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

        public DataTable MarcarPasoPorElPC(string idVotante, int idUsuarioMarca, string? observacion = null)
        {
            int filas = _data.MarcarPasoPorElPC(idVotante, idUsuarioMarca, observacion);
            var dt = new DataTable();
            dt.Columns.Add("FilasAfectadas", typeof(int));
            dt.Rows.Add(filas);
            return dt;
        }

        public DataTable MarcarCombustible(string idVotante, int idUsuarioMarca, bool recibioCombustible)
        {
            int filas = _data.MarcarCombustible(idVotante, idUsuarioMarca, recibioCombustible);
            var dt = new DataTable();
            dt.Columns.Add("FilasAfectadas", typeof(int));
            dt.Rows.Add(filas);
            return dt;
        }

        public DataTable RecintoDiadConteos(string? idRecinto = null, int? idAdmin = null, string? nroMesa = null, int? idTerritorio = null)
        {
            return _data.RecintoDiadConteos(idRecinto, idAdmin, nroMesa, idTerritorio);
        }

        public DataTable RecintoPadronFaltan(string? idRecinto = null, string? nroMesa = null, string? texto = null, int offset = 0, int limit = 100, int? idTerritorio = null)
        {
            return _data.RecintoPadronFaltan(idRecinto, nroMesa, texto, offset, limit, idTerritorio);
        }

        public DataTable RecintoVotaronNoRegistrados(string? idRecinto = null, string? nroMesa = null, string? texto = null, int offset = 0, int limit = 100, int? idTerritorio = null)
        {
            return _data.RecintoVotaronNoRegistrados(idRecinto, nroMesa, texto, offset, limit, idTerritorio);
        }

        public DataTable RecintoRegistradosFaltan(string? idRecinto = null, int? idAdmin = null, string? nroMesa = null, string? texto = null, int offset = 0, int limit = 100, int? idTerritorio = null)
        {
            return _data.RecintoRegistradosFaltan(idRecinto, idAdmin, nroMesa, texto, offset, limit, idTerritorio);
        }

        public DataTable RecintoMesas(string? idRecinto = null, int? idTerritorio = null)
        {
            return _data.RecintoMesas(idRecinto, idTerritorio);
        }

        public DataTable ObtenerTop10(int? idTerritorio = null)
        {
            return _data.ObtenerTop10(idTerritorio);
        }

        public DataTable ObtenerTop50(int? idTerritorio = null)
        {
            return _data.ObtenerTop10(idTerritorio);
        }

        public DataTable VeedoresRendimientoResumen(int? idTerritorio = null, int? idAdmin = null, int? idGerente = null)
        {
            return _data.VeedoresRendimientoResumen(idTerritorio, idAdmin, idGerente);
        }

        public DataTable VotantesMarcadosListar(
            int? idUsuarioMarca = null,
            string? tipoMarca = null,
            int? idTerritorio = null,
            int? idAdmin = null,
            int? idGerente = null,
            int? idMovilizador = null,
            string? texto = null,
            int offset = 0,
            int limit = 1000)
        {
            return _data.VotantesMarcadosListar(idUsuarioMarca, tipoMarca, idTerritorio, idAdmin, idGerente, idMovilizador, texto, offset, limit);
        }

        public int SuperAdminLimpiarMarcasDiaD(bool limpiarVoto, bool limpiarGasolina, bool limpiarPasoPC, int? idTerritorio = null)
        {
            return _data.SuperAdminLimpiarMarcasDiaD(limpiarVoto, limpiarGasolina, limpiarPasoPC, idTerritorio);
        }
    }
}
