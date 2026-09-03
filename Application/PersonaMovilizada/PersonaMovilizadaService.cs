using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Application.Configuracion;
using Application.WhatsApp;
using Infrastructure;
using Microsoft.Extensions.Configuration;

namespace Application.PersonaMovilizada
{
    public class PersonaMovilizadaService
    {
        private readonly DPersonaMovilizada _data = new DPersonaMovilizada();
        private readonly DMovilizadorMeta _metaData = new DMovilizadorMeta();
        private readonly ConfiguracionService _configuracionService = new ConfiguracionService();
        private readonly DUsuario _usuarios = new DUsuario();
        private readonly IConfiguration? _configuration;

        public PersonaMovilizadaService() { }

        public PersonaMovilizadaService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private void ValidarMetaMaxima(int idUsuarioMovilizador)
        {
            var (metaObjetivo, totalRegistrados) = _metaData.ObtenerMetaYTotalPersonas(idUsuarioMovilizador);
            if (totalRegistrados >= metaObjetivo)
            {
                throw new Exception($"El movilizador ya alcanzó su meta máxima permitida de {metaObjetivo} votantes registrados.");
            }
        }


        private void ValidarCamposObligatorios(
            int? idTerritorio,
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
            // Cada Admin Territorial tiene su propia configuración de campos obligatorios;
            // se usa el territorio de la persona que se está registrando (no el del caller,
            // que acá no está disponible: movilizador/gerente heredan el territorio del
            // Admin dueño de su estructura al crearse, ver UsuarioService.Insertar).
            var obligatorios = _configuracionService.ObtenerCamposObligatorios(idTerritorio);
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

        private const int MaxDuplicadosPermitidos = 2;

        /// <summary>
        /// Reglas de CI duplicado:
        ///  - Dentro del MISMO movilizador nunca se permite, esté o no habilitado
        ///    "permitir duplicados" a nivel de territorio.
        ///  - Entre movilizadores distintos: si el territorio no permite duplicados,
        ///    se bloquea igual que antes; si sí los permite, se tolera hasta
        ///    MaxDuplicadosPermitidos copias del mismo CI y se bloquea de ahí en más.
        /// </summary>
        private void ValidarDuplicadoCI(string? ci, int idUsuarioMovilizador, int? idTerritorio, int? excludeIdPersona)
        {
            if (string.IsNullOrWhiteSpace(ci)) return;

            var (total, enMismoMovilizador) = _data.ContarPorCI(ci.Trim(), idUsuarioMovilizador, excludeIdPersona);

            if (enMismoMovilizador > 0)
            {
                throw new Exception($"El CI '{ci.Trim()}' ya está registrado en tu propia lista. No se puede duplicar dentro del mismo movilizador.");
            }

            bool permitirDuplicados = _configuracionService.ObtenerPermitirDuplicados(idTerritorio);
            if (!permitirDuplicados)
            {
                if (total > 0)
                {
                    throw new Exception($"El CI '{ci.Trim()}' ya fue registrado por otra persona. No se permiten votantes duplicados.");
                }
            }
            else if (total >= MaxDuplicadosPermitidos)
            {
                throw new Exception($"El CI '{ci.Trim()}' ya alcanzó el máximo de {MaxDuplicadosPermitidos} registros permitidos entre distintos movilizadores.");
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
            ValidarMetaMaxima(idUsuarioMovilizador);

            ValidarDuplicadoCI(ci, idUsuarioMovilizador, idTerritorio, excludeIdPersona: null);

            ValidarCamposObligatorios(
                idTerritorio,
                celular, direccionReferencia, sexo, rangoEdad,
                recintoVotacion, idRecinto, nivelCompromiso, observaciones,
                latitud, longitud);


            var resultado = _data.Insertar(
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

            // Best-effort, sin bloquear la respuesta del registro: si el envío
            // falla (sesión desconectada, celular inválido, etc.) no debe
            // afectar el alta de la persona, que ya se guardó bien.
            if (resultado != null && resultado.Rows.Count > 0 && !string.IsNullOrWhiteSpace(celular))
            {
                _ = EnviarBienvenidaMovilizadorAsync(idUsuarioMovilizador, idTerritorio, nombres, celular);
            }

            return resultado;
        }

        private async Task EnviarBienvenidaMovilizadorAsync(int idUsuarioMovilizador, int? idTerritorio, string nombrePersona, string celular)
        {
            if (_configuration == null) return;

            try
            {
                var whatsApp = new WhatsAppService(_configuration);
                var botConfig = whatsApp.ObtenerBotConfiguracion(idTerritorio);
                if (string.IsNullOrWhiteSpace(botConfig.MensajeBienvenidaMovilizador)) return;

                string nombreMovilizador = "tu movilizador";
                var datosUsuario = _usuarios.ObtenerPorId(idUsuarioMovilizador);
                if (datosUsuario != null && datosUsuario.Rows.Count > 0 && datosUsuario.Columns.Contains("NombreCompleto"))
                {
                    var valor = datosUsuario.Rows[0]["NombreCompleto"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(valor)) nombreMovilizador = valor;
                }

                string mensaje = botConfig.MensajeBienvenidaMovilizador
                    .Replace("{nombre}", nombrePersona)
                    .Replace("{movilizador}", nombreMovilizador)
                    .Replace("{candidato}", botConfig.NombreCandidato);

                await whatsApp.EnviarMensajeAUsuarioAsync(idUsuarioMovilizador, celular, mensaje);
            }
            catch
            {
                // Best-effort: un fallo acá nunca debe afectar el registro de la persona.
            }
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
            ValidarDuplicadoCI(ci, idUsuarioMovilizador, idTerritorio, excludeIdPersona: idPersonaMovilizada);

            ValidarCamposObligatorios(
                idTerritorio,
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

        public DataTable BuscarGeneral(int? idTerritorio, int? idUsuarioMovilizador, string? texto, string? estadoDiaD, string? estadoApoyo = null)
        {
            return _data.BuscarGeneral(idTerritorio, idUsuarioMovilizador, texto, estadoDiaD, estadoApoyo);
        }

        public DataTable ResumenMovilizador(int idUsuarioMovilizador)
        {
            return _data.ResumenMovilizador(idUsuarioMovilizador);
        }

        public DataTable CelularesRepetidos(int? idTerritorio, int? idUsuarioMovilizador)
        {
            return _data.CelularesRepetidos(idTerritorio, idUsuarioMovilizador);
        }
    }
}