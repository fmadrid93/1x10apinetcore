using System;
using System.Collections.Generic;
using System.Data;
using Application.Configuracion;
using Infrastructure;

namespace Application.PersonaMovilizada
{
    public class PersonaMovilizadaService
    {
        private readonly DPersonaMovilizada _data = new DPersonaMovilizada();
        private readonly DConfiguracion _config = new DConfiguracion();
        private readonly ConfiguracionService _configuracionService = new ConfiguracionService();

        private void ValidarCamposObligatorios(
            string? celular,
            string? direccionReferencia,
            string? sexo,
            string? rangoEdad,
            string? recintoVotacion,
            string? idRecinto,
            string? nivelCompromiso,
            string? observaciones,
            decimal? latitud,
            decimal? longitud)
        {
            var obligatorios = _configuracionService.ObtenerCamposObligatorios();
            if (obligatorios.Count == 0) return;

            var valores = new Dictionary<string, bool>
            {
                ["CELULAR"] = !string.IsNullOrWhiteSpace(celular),
                ["DIRECCION"] = !string.IsNullOrWhiteSpace(direccionReferencia),
                ["SEXO"] = !string.IsNullOrWhiteSpace(sexo),
                ["RANGO_EDAD"] = !string.IsNullOrWhiteSpace(rangoEdad),
                ["RECINTO"] = !string.IsNullOrWhiteSpace(recintoVotacion) || !string.IsNullOrWhiteSpace(idRecinto),
                ["NIVEL_COMPROMISO"] = !string.IsNullOrWhiteSpace(nivelCompromiso),
                ["OBSERVACIONES"] = !string.IsNullOrWhiteSpace(observaciones),
                ["UBICACION"] = latitud.HasValue && longitud.HasValue,
            };

            var faltantes = new List<string>();
            foreach (var codigo in obligatorios)
            {
                if (valores.TryGetValue(codigo, out bool tieneValor) && !tieneValor)
                {
                    faltantes.Add(codigo);
                }
            }

            if (faltantes.Count > 0)
            {
                var etiquetas = new List<string>();
                foreach (var codigo in faltantes)
                {
                    foreach (var campo in CamposVotanteCatalogo.CamposConfigurables)
                    {
                        if (campo.Codigo == codigo)
                        {
                            etiquetas.Add(campo.Etiqueta);
                            break;
                        }
                    }
                }
                throw new Exception($"Los siguientes campos son obligatorios: {string.Join(", ", etiquetas)}.");
            }
        }

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

            ValidarCamposObligatorios(
                celular, direccionReferencia, sexo, rangoEdad,
                recintoVotacion, idRecinto, nivelCompromiso, observaciones,
                latitud, longitud);

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

            ValidarCamposObligatorios(
                celular, direccionReferencia, sexo, rangoEdad,
                recintoVotacion, idRecinto, nivelCompromiso, observaciones,
                latitud, longitud);

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