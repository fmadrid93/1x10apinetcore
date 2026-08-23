using System.Data;
using Infrastructure;

namespace Application.PersonaMovilizada
{
    public class PersonaMovilizadaService
    {
        private readonly DPersonaMovilizada _data = new DPersonaMovilizada();
        private readonly DConfiguracion _config = new DConfiguracion();

        public DataTable Insertar(
         int idUsuarioMovilizador,
         int? idTerritorio,
         string nombres,
         string apellidos,
         string? ci,
         string? celular,
         string? direccionReferencia,
         string? sexo,
         string? rangoEdad,
         string? recintoVotacion,
          string? idRecinto,
         bool? requiereAyudaVotar,
         string? nivelCompromiso,
         string? observaciones,
         decimal? latitud,
         decimal? longitud
     )
        {
            if (!string.IsNullOrWhiteSpace(ci))
            {
                bool permitirDuplicados = _config.ObtenerPermitirDuplicados();
                if (!permitirDuplicados)
                {
                    var existente = _data.VerificarExisteCI(ci.Trim());
                    if (existente != null && existente.Rows.Count > 0)
                    {
                        throw new Exception($"El CI '{ci.Trim()}' ya fue registrado por otra persona. No se permiten votantes duplicados.");
                    }
                }
            }

            return _data.Insertar(
                idUsuarioMovilizador,
                idTerritorio,
                nombres,
                apellidos,
                ci,
                celular,
                direccionReferencia,
                sexo,
                rangoEdad,
                recintoVotacion,
                idRecinto,
                requiereAyudaVotar,
                nivelCompromiso,
                observaciones,
                latitud,
                longitud
            );
        }

        public DataTable Actualizar(
            int idPersonaMovilizada,
            int idUsuarioMovilizador,
            int? idTerritorio,
            string nombres,
            string apellidos,
            string? ci,
            string? celular,
            string? direccionReferencia,
            string? sexo,
            string? rangoEdad,
            string? recintoVotacion,
             string? idRecinto,
            bool? requiereAyudaVotar,
            string? nivelCompromiso,
            string? observaciones,
            decimal? latitud,
            decimal? longitud
        )
        {
            if (!string.IsNullOrWhiteSpace(ci))
            {
                bool permitirDuplicados = _config.ObtenerPermitirDuplicados();
                if (!permitirDuplicados)
                {
                    var existente = _data.VerificarExisteCI(ci.Trim(), idPersonaMovilizada);
                    if (existente != null && existente.Rows.Count > 0)
                    {
                        throw new Exception($"El CI '{ci.Trim()}' ya fue registrado por otra persona. No se permiten votantes duplicados.");
                    }
                }
            }

            return _data.Actualizar(
                idPersonaMovilizada,
                idUsuarioMovilizador,
                idTerritorio,
                nombres,
                apellidos,
                ci,
                celular,
                direccionReferencia,
                sexo,
                rangoEdad,
                recintoVotacion,
              idRecinto,
                requiereAyudaVotar,
                nivelCompromiso,
                observaciones,
                latitud,
                longitud
            );
        }

        public DataTable EliminarLogico(int idPersonaMovilizada, int idUsuarioMovilizador)
        {
            return _data.EliminarLogico(idPersonaMovilizada, idUsuarioMovilizador);
        }

        public DataTable ObtenerPorId(int idPersonaMovilizada)
        {
            return _data.ObtenerPorId(idPersonaMovilizada);
        }

        public DataTable ListarPorMovilizador(int idUsuarioMovilizador, string? texto, string? estadoDiaD)
        {
            return _data.ListarPorMovilizador(idUsuarioMovilizador, texto, estadoDiaD);
        }

        public DataTable BuscarGeneral(int? idTerritorio, int? idUsuarioMovilizador, string? texto, string? estadoDiaD)
        {
            return _data.BuscarGeneral(idTerritorio, idUsuarioMovilizador, texto, estadoDiaD);
        }

        public DataTable ResumenMovilizador(int idUsuarioMovilizador)
        {
            return _data.ResumenMovilizador(idUsuarioMovilizador);
        }
    }
}