using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Dtos.WhatsApp;
using Infrastructure;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Application.WhatsApp
{
    public class WhatsAppService
    {
        private readonly DWhatsApp _dWhatsApp = new DWhatsApp();
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        private string SaasBaseUrl => _configuration["WhatsAppSettings:SaasBaseUrl"] ?? "http://127.0.0.1:3000/api";
        private string BridgeBaseUrl => _configuration["WhatsAppSettings:BridgeBaseUrl"] ?? "http://127.0.0.1:3001";
        public bool HabilitarEnlaceQr => bool.TryParse(_configuration["WhatsAppSettings:HabilitarEnlaceQr"], out bool val) ? val : true;
        public string StorageMode => _configuration["WhatsAppSettings:StorageMode"] ?? "DISK";

        public WhatsAppService(IConfiguration configuration)
        {
            _configuration = configuration;
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        }

        /// <summary>
        /// Obtiene la URL del servidor de WhatsApp correspondiente al usuario según su territorio/municipio.
        /// Si no tiene una ruta configurada en el territorio, usa la URL por defecto configurada en appsettings.
        /// </summary>
        public string ObtenerUrlServidorParaUsuario(int idUsuario)
        {
            try
            {
                string? urlTerritorio = _dWhatsApp.ObtenerUrlServidorWhatsAppPorUsuario(idUsuario);
                if (!string.IsNullOrEmpty(urlTerritorio))
                {
                    return urlTerritorio.TrimEnd('/');
                }
            }
            catch { }

            return BridgeBaseUrl.TrimEnd('/');
        }

        public async Task<WhatsAppConfigDto> ObtenerConfiguracionAsync(int? idUsuario = null)
        {
            string baseUrl = idUsuario.HasValue ? ObtenerUrlServidorParaUsuario(idUsuario.Value) : BridgeBaseUrl;
            string ip = await ObtenerIpSalidaAsync(baseUrl);

            return new WhatsAppConfigDto
            {
                HabilitarEnlaceQr = this.HabilitarEnlaceQr,
                StorageMode = this.StorageMode,
                SaasBaseUrl = baseUrl,
                OutboundIp = ip
            };
        }

        public async Task<string> ObtenerIpSalidaAsync(string? customBaseUrl = null)
        {
            string urlBridge = customBaseUrl ?? BridgeBaseUrl;

            try
            {
                // Intentar consultar endpoint de IP del servidor de WhatsApp correspondiente
                var resp = await _httpClient.GetAsync($"{urlBridge}/system/public-ip");
                if (resp.IsSuccessStatusCode)
                {
                    var json = await resp.Content.ReadAsStringAsync();
                    var obj = JObject.Parse(json);
                    if (obj["ip"] != null) return obj["ip"].ToString();
                }
            }
            catch { }

            try
            {
                // Fallback a servicio público directo
                var resp = await _httpClient.GetAsync("https://api.ipify.org?format=json");
                if (resp.IsSuccessStatusCode)
                {
                    var json = await resp.Content.ReadAsStringAsync();
                    var obj = JObject.Parse(json);
                    if (obj["ip"] != null) return obj["ip"].ToString();
                }
            }
            catch { }

            return "127.0.0.1 (Local)";
        }

        public async Task<List<WhatsAppSessionDto>> ListarSesionesUsuarioAsync(int idUsuario)
        {
            var sesiones = new List<WhatsAppSessionDto>();
            string serverUrl = ObtenerUrlServidorParaUsuario(idUsuario);
            string ip = await ObtenerIpSalidaAsync(serverUrl);
            string prefijo = $"u{idUsuario}_";

            // Intentar listar desde el servidor de WhatsApp del municipio/territorio
            try
            {
                var resp = await _httpClient.GetAsync($"{serverUrl}/sessions");
                if (resp.IsSuccessStatusCode)
                {
                    var json = await resp.Content.ReadAsStringAsync();
                    var obj = JObject.Parse(json);
                    var list = obj["sessions"] as JArray;
                    if (list != null)
                    {
                        foreach (var item in list)
                        {
                            string sId = item.ToString();
                            if (sId.StartsWith(prefijo) || sId == $"u{idUsuario}" || sId == "default")
                            {
                                var statusDto = await ObtenerEstadoSesionBridgeAsync(serverUrl, sId, ip);
                                sesiones.Add(statusDto);
                            }
                        }
                    }
                }
            }
            catch { }

            // Si no hay sesiones activas, registrar al menos la sesión primaria
            if (sesiones.Count == 0)
            {
                string sessionId = $"u{idUsuario}_principal";
                var status = await ObtenerEstadoSesionBridgeAsync(serverUrl, sessionId, ip);
                sesiones.Add(status);
            }

            return sesiones;
        }

        private async Task<WhatsAppSessionDto> ObtenerEstadoSesionBridgeAsync(string serverUrl, string sessionId, string ip)
        {
            var dto = new WhatsAppSessionDto
            {
                Id = sessionId,
                Name = sessionId.Replace("_", " ").ToUpper(),
                Status = "DISCONNECTED",
                OutboundIp = ip
            };

            try
            {
                var resp = await _httpClient.GetAsync($"{serverUrl}/session/{sessionId}/status");
                if (resp.IsSuccessStatusCode)
                {
                    var json = await resp.Content.ReadAsStringAsync();
                    var obj = JObject.Parse(json);
                    bool connected = obj["connected"]?.Value<bool>() ?? false;
                    string state = obj["state"]?.ToString() ?? "stopped";

                    dto.Status = connected ? "CONNECTED" : (state == "qr" ? "QR" : (state == "connecting" ? "CONNECTING" : "DISCONNECTED"));
                    dto.PhoneE164 = obj["me"]?.ToString();
                    if (!string.IsNullOrEmpty(dto.PhoneE164) && !dto.PhoneE164.StartsWith("+"))
                    {
                        dto.PhoneE164 = "+" + dto.PhoneE164;
                    }
                }
            }
            catch { }

            return dto;
        }

        public async Task<WhatsAppSessionDto> CrearSesionAsync(int idUsuario, string? alias)
        {
            if (!HabilitarEnlaceQr)
            {
                throw new Exception("El enlace de nuevos códigos QR se encuentra temporalmente deshabilitado por configuración.");
            }

            string serverUrl = ObtenerUrlServidorParaUsuario(idUsuario);
            string sufijo = string.IsNullOrEmpty(alias) ? DateTime.Now.Ticks.ToString().Substring(12) : alias.Trim().Replace(" ", "_").ToLower();
            string sessionId = $"u{idUsuario}_{sufijo}";

            try
            {
                var content = new StringContent("{}", Encoding.UTF8, "application/json");
                await _httpClient.PostAsync($"{serverUrl}/session/{sessionId}/start", content);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al iniciar nueva sesión de WhatsApp en {serverUrl}: {ex.Message}");
            }

            string ip = await ObtenerIpSalidaAsync(serverUrl);
            return await ObtenerEstadoSesionBridgeAsync(serverUrl, sessionId, ip);
        }

        public async Task<object> ObtenerQrAsync(string sessionId, int? idUsuario = null)
        {
            string serverUrl = idUsuario.HasValue ? ObtenerUrlServidorParaUsuario(idUsuario.Value) : BridgeBaseUrl;

            // Si no se pasó idUsuario, intentar inferirlo del sessionId (ej: "u25_principal" -> 25)
            if (!idUsuario.HasValue && sessionId.StartsWith("u"))
            {
                int underscoreIdx = sessionId.IndexOf('_');
                if (underscoreIdx > 1)
                {
                    if (int.TryParse(sessionId.Substring(1, underscoreIdx - 1), out int parsedId))
                    {
                        serverUrl = ObtenerUrlServidorParaUsuario(parsedId);
                    }
                }
            }

            try
            {
                // Asegurar que la sesión esté iniciada en el servidor de destino
                var startContent = new StringContent("{}", Encoding.UTF8, "application/json");
                await _httpClient.PostAsync($"{serverUrl}/session/{sessionId}/start", startContent);

                var resp = await _httpClient.GetAsync($"{serverUrl}/session/{sessionId}/qr");
                if (resp.IsSuccessStatusCode)
                {
                    var json = await resp.Content.ReadAsStringAsync();
                    var obj = JObject.Parse(json);

                    bool available = obj["available"]?.Value<bool>() ?? false;
                    string? qrPngBase64 = obj["qrPngBase64"]?.ToString();

                    // Revisar status por si ya se conectó
                    var statusResp = await _httpClient.GetAsync($"{serverUrl}/session/{sessionId}/status");
                    var statusJson = await statusResp.Content.ReadAsStringAsync();
                    var statusObj = JObject.Parse(statusJson);

                    bool connected = statusObj["connected"]?.Value<bool>() ?? false;
                    string phone = statusObj["me"]?.ToString() ?? "";

                    return new
                    {
                        sessionId,
                        serverUrl,
                        available,
                        connected,
                        phone = connected ? (phone.StartsWith("+") ? phone : "+" + phone) : null,
                        qrPngBase64 = available ? qrPngBase64 : null,
                        qrDataUrl = available && !string.IsNullOrEmpty(qrPngBase64) ? $"data:image/png;base64,{qrPngBase64}" : null
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al consultar QR en {serverUrl}: {ex.Message}");
            }

            return new { sessionId, serverUrl, available = false, connected = false, qrPngBase64 = (string?)null };
        }

        public async Task<bool> EliminarSesionAsync(string sessionId, int? idUsuario = null)
        {
            string serverUrl = idUsuario.HasValue ? ObtenerUrlServidorParaUsuario(idUsuario.Value) : BridgeBaseUrl;
            if (!idUsuario.HasValue && sessionId.StartsWith("u"))
            {
                int underscoreIdx = sessionId.IndexOf('_');
                if (underscoreIdx > 1 && int.TryParse(sessionId.Substring(1, underscoreIdx - 1), out int parsedId))
                {
                    serverUrl = ObtenerUrlServidorParaUsuario(parsedId);
                }
            }

            try
            {
                var content = new StringContent("{}", Encoding.UTF8, "application/json");
                await _httpClient.PostAsync($"{serverUrl}/session/{sessionId}/reset", content);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public List<WhatsAppDestinatarioDto> ObtenerDestinatariosNodo(int idUsuario, string rol)
        {
            DataTable dt = _dWhatsApp.ObtenerPersonasPorNodo(idUsuario, rol);
            var lista = new List<WhatsAppDestinatarioDto>();

            if (dt != null && dt.Rows.Count > 0)
            {
                foreach (DataRow row in dt.Rows)
                {
                    lista.Add(new WhatsAppDestinatarioDto
                    {
                        IdPersonaMovilizada = Convert.ToInt32(row["IdPersonaMovilizada"]),
                        IdUsuarioMovilizador = row["IdUsuarioMovilizador"] == DBNull.Value ? 0 : Convert.ToInt32(row["IdUsuarioMovilizador"]),
                        IdUsuarioSupervisor = row.Table.Columns.Contains("IdUsuarioSupervisor") && row["IdUsuarioSupervisor"] != DBNull.Value ? (int?)Convert.ToInt32(row["IdUsuarioSupervisor"]) : null,
                        Nombres = row["Nombres"]?.ToString() ?? string.Empty,
                        Apellidos = row["Apellidos"]?.ToString() ?? string.Empty,
                        CI = row["CI"] == DBNull.Value ? null : row["CI"]?.ToString(),
                        Celular = row["Celular"]?.ToString() ?? string.Empty,
                        RecintoVotacion = row["RecintoVotacion"] == DBNull.Value ? null : row["RecintoVotacion"]?.ToString(),
                        EstadoDiaD = row["EstadoDiaD"] == DBNull.Value ? null : row["EstadoDiaD"]?.ToString(),
                        NombreMovilizador = row.Table.Columns.Contains("NombreMovilizador") && row["NombreMovilizador"] != DBNull.Value ? row["NombreMovilizador"]?.ToString() : null
                    });
                }
            }

            return lista;
        }

        public async Task<WhatsAppDifusionResultDto> EnviarDifusionNodoAsync(WhatsAppDifusionRequest request)
        {
            var destinatariosBrutos = ObtenerDestinatariosNodo(request.IdUsuario, request.Rol);

            // 1. Filtro estricto anti-duplicados por número de celular (0 duplicados)
            var celularesVistos = new HashSet<string>();
            var destinatarios = new List<WhatsAppDestinatarioDto>();
            foreach (var d in destinatariosBrutos)
            {
                string normCel = NormalizarCelular(d.Celular);
                if (!string.IsNullOrEmpty(normCel) && celularesVistos.Add(normCel))
                {
                    destinatarios.Add(d);
                }
            }

            if (destinatarios.Count == 0)
            {
                return new WhatsAppDifusionResultDto
                {
                    TotalDestinatarios = 0,
                    EncoladosCorrectamente = 0,
                    Fallidos = 0,
                    MensajeEstado = "No se encontraron personas movilizadas con número de celular en tu nodo."
                };
            }

            // Si se solicitó programar para una fecha/hora futura
            if (request.FechaProgramada.HasValue && request.FechaProgramada.Value > DateTime.Now.AddMinutes(1))
            {
                string sessionsJson = request.SessionIdsSeleccionadas != null ? JsonConvert.SerializeObject(request.SessionIdsSeleccionadas) : null;
                _dWhatsApp.GuardarCampanaProgramada(
                    request.IdUsuario,
                    request.Rol,
                    request.Mensaje,
                    request.FechaProgramada.Value,
                    sessionsJson,
                    destinatarios.Count
                );

                return new WhatsAppDifusionResultDto
                {
                    TotalDestinatarios = destinatarios.Count,
                    EncoladosCorrectamente = destinatarios.Count,
                    Fallidos = 0,
                    EsProgramado = true,
                    FechaProgramada = request.FechaProgramada.Value,
                    MensajeEstado = $"Campaña de difusión programada exitosamente para el {request.FechaProgramada.Value:dd/MM/yyyy HH:mm} para {destinatarios.Count} destinatarios únicos."
                };
            }

            string serverUrl = ObtenerUrlServidorParaUsuario(request.IdUsuario);

            // Obtener sesiones activas en el servidor correspondiente a su territorio
            var todasSesiones = await ListarSesionesUsuarioAsync(request.IdUsuario);
            var sesionesConectadas = todasSesiones.FindAll(s => s.Status == "CONNECTED");

            // Si especificó sesiones preferidas, filtrar por esas
            if (request.SessionIdsSeleccionadas != null && request.SessionIdsSeleccionadas.Count > 0)
            {
                sesionesConectadas = sesionesConectadas.FindAll(s => request.SessionIdsSeleccionadas.Contains(s.Id));
            }

            if (sesionesConectadas.Count == 0)
            {
                throw new Exception($"No tienes ninguna sesión de WhatsApp conectada en el servidor ({serverUrl}). Ve a 'Mis Sesiones WhatsApp' y vincula tu número mediante el código QR antes de enviar.");
            }

            // 2. Mapear dueño de cada sesión conectada (u{IdUsuario}_...)
            int ExtraerIdUsuarioSesion(WhatsAppSessionDto sesion)
            {
                if (sesion.Name.StartsWith("u") || sesion.Id.StartsWith("u"))
                {
                    string refStr = sesion.Name.StartsWith("u") ? sesion.Name : sesion.Id;
                    int idx = refStr.IndexOf('_');
                    if (idx > 1 && int.TryParse(refStr.Substring(1, idx - 1), out int uid))
                    {
                        return uid;
                    }
                }
                return request.IdUsuario;
            }

            var sesionesPorMovilizador = new Dictionary<int, List<WhatsAppSessionDto>>();
            foreach (var s in sesionesConectadas)
            {
                int uid = ExtraerIdUsuarioSesion(s);
                if (!sesionesPorMovilizador.ContainsKey(uid))
                {
                    sesionesPorMovilizador[uid] = new List<WhatsAppSessionDto>();
                }
                sesionesPorMovilizador[uid].Add(s);
            }

            // 3. Organizar los destinatarios en 3 capas de prioridad
            // Capa 1: Contactos cuyo movilizador TIENE sesión conectada (Afinidad Directa)
            // Capa 2: Contactos de movilizadores del equipo del Gerente que no tienen sesión
            // Capa 3: Resto del padrón de la estructura del Administrador
            var colaAfinidadDirecta = new Dictionary<int, List<WhatsAppDestinatarioDto>>();
            var colaEquipoGerente = new List<WhatsAppDestinatarioDto>();
            var colaGeneralAdmin = new List<WhatsAppDestinatarioDto>();

            foreach (var dest in destinatarios)
            {
                if (sesionesPorMovilizador.ContainsKey(dest.IdUsuarioMovilizador))
                {
                    if (!colaAfinidadDirecta.ContainsKey(dest.IdUsuarioMovilizador))
                    {
                        colaAfinidadDirecta[dest.IdUsuarioMovilizador] = new List<WhatsAppDestinatarioDto>();
                    }
                    colaAfinidadDirecta[dest.IdUsuarioMovilizador].Add(dest);
                }
                else if (dest.IdUsuarioSupervisor.HasValue && dest.IdUsuarioSupervisor.Value == request.IdUsuario)
                {
                    colaEquipoGerente.Add(dest);
                }
                else
                {
                    colaGeneralAdmin.Add(dest);
                }
            }

            int exitosos = 0;
            int fallidos = 0;
            var sesionesUsadas = new HashSet<string>();

            // Función auxiliar de despacho individual
            async Task EnviarAContactoAsync(WhatsAppDestinatarioDto dest, WhatsAppSessionDto sesion)
            {
                string celular = NormalizarCelular(dest.Celular);
                if (string.IsNullOrEmpty(celular))
                {
                    fallidos++;
                    return;
                }

                sesionesUsadas.Add(sesion.Id);

                string textoFinal = request.Mensaje
                    .Replace("{nombre}", dest.Nombres)
                    .Replace("{apellido}", dest.Apellidos)
                    .Replace("{candidato}", request.NombreCandidato ?? "nuestro candidato")
                    .Replace("{recinto}", dest.RecintoVotacion ?? "");

                try
                {
                    bool enviado = await EnviarMensajeBridgeAsync(serverUrl, sesion.Id, celular, textoFinal);
                    if (enviado) exitosos++;
                    else fallidos++;
                    await Task.Delay(new Random().Next(1000, 2200));
                }
                catch
                {
                    fallidos++;
                }
            }


            // FASE 1: Cada movilizador envía PRIMERO a su propia gente 1x10 (haciendo Round-Robin entre sus propias líneas si tiene varias)
            foreach (var kvp in colaAfinidadDirecta)
            {
                int idMov = kvp.Key;
                var contactosPropios = kvp.Value;
                var lineasDelMovilizador = sesionesPorMovilizador[idMov];

                int idxLinea = 0;
                foreach (var dest in contactosPropios)
                {
                    var sesion = lineasDelMovilizador[idxLinea % lineasDelMovilizador.Count];
                    idxLinea++;
                    await EnviarAContactoAsync(dest, sesion);
                }
            }

            // FASE 2: Colaboración en Round-Robin para la gente del Gerente que no tenía sesión propia
            if (colaEquipoGerente.Count > 0)
            {
                int idxGerente = 0;
                foreach (var dest in colaEquipoGerente)
                {
                    var sesion = sesionesConectadas[idxGerente % sesionesConectadas.Count];
                    idxGerente++;
                    await EnviarAContactoAsync(dest, sesion);
                }
            }

            // FASE 3: Colaboración en Round-Robin para el padrón general de la estructura del Administrador
            if (colaGeneralAdmin.Count > 0)
            {
                int idxAdmin = 0;
                foreach (var dest in colaGeneralAdmin)
                {
                    var sesion = sesionesConectadas[idxAdmin % sesionesConectadas.Count];
                    idxAdmin++;
                    await EnviarAContactoAsync(dest, sesion);
                }
            }

            if (request.EsBotEncuesta && exitosos > 0)
            {
                var celularesEnviados = destinatarios.ConvertAll(d => NormalizarCelular(d.Celular));
                _dWhatsApp.MarcarComoConsultadosPorCelulares(string.Join(",", celularesEnviados));
            }

            return new WhatsAppDifusionResultDto
            {
                TotalDestinatarios = destinatarios.Count,
                EncoladosCorrectamente = exitosos,
                Fallidos = fallidos,
                SesionesUtilizadas = new List<string>(sesionesUsadas),
                MensajeEstado = $"Envío completado: {exitosos} mensaje(s) procesados por prioridad jerárquica a través de {sesionesUsadas.Count} cuenta(s) en servidor ({serverUrl}). Cero duplicados."
            };
        }


        public WhatsAppBotConfigDto ObtenerBotConfiguracion(string? nombreCandidato = null)
        {
            try
            {
                DataTable dt = _dWhatsApp.ObtenerBotConfiguracionBD();
                if (dt != null && dt.Rows.Count > 0)
                {
                    var r = dt.Rows[0];
                    return new WhatsAppBotConfigDto
                    {
                        IdBot = Convert.ToInt32(r["IdBot"]),
                        Titulo = r["Titulo"]?.ToString() ?? "Consulta de Intención de Voto",
                        NombreCandidato = !string.IsNullOrWhiteSpace(nombreCandidato) ? nombreCandidato : (r["NombreCandidato"]?.ToString() ?? "nuestro candidato"),
                        PlantillaPregunta = r["PlantillaPregunta"]?.ToString() ?? "",
                        Opcion1_Texto = r["Opcion1_Texto"]?.ToString() ?? "Sí, totalmente",
                        Opcion1_EstadoApoyo = r["Opcion1_EstadoApoyo"]?.ToString() ?? "APOYA",
                        Opcion1_Respuesta = r["Opcion1_Respuesta"]?.ToString() ?? "",
                        Opcion2_Texto = r["Opcion2_Texto"]?.ToString() ?? "Tal vez / Indeciso",
                        Opcion2_EstadoApoyo = r["Opcion2_EstadoApoyo"]?.ToString() ?? "CONSULTADO",
                        Opcion2_Respuesta = r["Opcion2_Respuesta"]?.ToString() ?? "",
                        Opcion3_Texto = r["Opcion3_Texto"]?.ToString() ?? "No",
                        Opcion3_EstadoApoyo = r["Opcion3_EstadoApoyo"]?.ToString() ?? "NO_APOYA",
                        Opcion3_Respuesta = r["Opcion3_Respuesta"]?.ToString() ?? "",
                        Activo = r["Activo"] == DBNull.Value || Convert.ToBoolean(r["Activo"])
                    };
                }
            }
            catch { }

            string candidato = string.IsNullOrWhiteSpace(nombreCandidato) ? "nuestro candidato" : nombreCandidato.Trim();
            return new WhatsAppBotConfigDto
            {
                IdBot = 1,
                Titulo = "Consulta de Intención de Voto",
                NombreCandidato = candidato,
                PlantillaPregunta = $"Hola {{nombre}}, ¿apoyarás a {candidato} en las próximas elecciones?\n\n1️⃣ Sí, totalmente\n2️⃣ Tal vez / Indeciso\n3️⃣ No\n\nPor favor responde con el número 1, 2 o 3.",
                Opcion1_Texto = "Sí, totalmente",
                Opcion1_EstadoApoyo = "APOYA",
                Opcion1_Respuesta = $"¡Excelente {{nombre}}! Muchísimas gracias por tu respaldo a {candidato}. ¡Juntos vamos a ganar!",
                Opcion2_Texto = "Tal vez / Indeciso",
                Opcion2_EstadoApoyo = "CONSULTADO",
                Opcion2_Respuesta = $"Gracias {{nombre}}. Te compartiremos nuestras principales propuestas para que conozcas a detalle el plan de trabajo de {candidato}.",
                Opcion3_Texto = "No",
                Opcion3_EstadoApoyo = "NO_APOYA",
                Opcion3_Respuesta = "Comprendemos tu postura, {nombre}. Agradecemos mucho tu sinceridad y tiempo. ¡Que tengas un excelente día!",
                Activo = true
            };
        }

        public WhatsAppBotConfigDto GuardarBotConfiguracion(WhatsAppBotConfigDto bot)
        {
            DataTable dt = _dWhatsApp.GuardarBotConfiguracionBD(
                bot.IdBot,
                bot.Titulo,
                bot.NombreCandidato,
                bot.PlantillaPregunta,
                bot.Opcion1_Texto,
                bot.Opcion1_EstadoApoyo,
                bot.Opcion1_Respuesta,
                bot.Opcion2_Texto,
                bot.Opcion2_EstadoApoyo,
                bot.Opcion2_Respuesta,
                bot.Opcion3_Texto,
                bot.Opcion3_EstadoApoyo,
                bot.Opcion3_Respuesta,
                bot.Activo
            );
            return ObtenerBotConfiguracion(bot.NombreCandidato);
        }

        public WhatsAppBotRespuestaResultDto ProcesarRespuestaBot(WhatsAppBotRespuestaRequest request)
        {
            string texto = (request.TextoRespuesta ?? "").Trim().ToLower();
            var botCfg = ObtenerBotConfiguracion();

            string estadoApoyo = "CONSULTADO";
            string mensajeRespuesta = botCfg.Opcion2_Respuesta;
            bool reconocido = false;

            if (texto.Contains("1") || texto.Contains("si") || texto.Contains("sí") || texto.Contains("totalmente") || texto.Contains("apoyo") || texto.Contains("claro") || texto.Contains("ganar"))
            {
                estadoApoyo = "APOYA";
                mensajeRespuesta = botCfg.Opcion1_Respuesta;
                reconocido = true;
            }
            else if (texto.Contains("2") || texto.Contains("tal vez") || texto.Contains("indeciso") || texto.Contains("duda") || texto.Contains("quizas") || texto.Contains("quizás") || texto.Contains("veremos"))
            {
                estadoApoyo = "CONSULTADO";
                mensajeRespuesta = botCfg.Opcion2_Respuesta;
                reconocido = true;
            }
            else if (texto.Contains("3") || texto.Contains("no") || texto.Contains("nunca") || texto.Contains("ninguno") || texto.Contains("jamas") || texto.Contains("jamás"))
            {
                estadoApoyo = "NO_APOYA";
                mensajeRespuesta = botCfg.Opcion3_Respuesta;
                reconocido = true;
            }

            int? idPersona = null;
            string? nombreVotante = null;

            try
            {
                DataTable dt = _dWhatsApp.ActualizarCompromisoPorCelular(request.Celular, estadoApoyo, request.TextoRespuesta);
                if (dt != null && dt.Rows.Count > 0)
                {
                    idPersona = Convert.ToInt32(dt.Rows[0]["IdPersonaMovilizada"]);
                    string nom = dt.Rows[0]["Nombres"]?.ToString() ?? "";
                    string ape = dt.Rows[0]["Apellidos"]?.ToString() ?? "";
                    nombreVotante = $"{nom} {ape}".Trim();

                    // Reemplazar variable {nombre} en la respuesta diferenciada
                    mensajeRespuesta = mensajeRespuesta
                        .Replace("{nombre}", nom)
                        .Replace("{apellido}", ape)
                        .Replace("{candidato}", botCfg.NombreCandidato);
                }
                else
                {
                    mensajeRespuesta = mensajeRespuesta
                        .Replace("{nombre}", "")
                        .Replace("{apellido}", "")
                        .Replace("{candidato}", botCfg.NombreCandidato)
                        .Trim();
                }
            }
            catch { }

            return new WhatsAppBotRespuestaResultDto
            {
                Reconocido = reconocido,
                IdPersonaMovilizada = idPersona,
                NombreVotante = nombreVotante,
                EstadoApoyoAsignado = estadoApoyo,
                MensajeRespuesta = mensajeRespuesta
            };
        }

        private async Task<bool> EnviarMensajeBridgeAsync(string serverUrl, string sessionId, string celular, string mensaje)
        {
            try
            {
                var payload = new { to = celular, message = mensaje };
                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var resp = await _httpClient.PostAsync($"{serverUrl}/session/{sessionId}/send", content);
                return resp.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<WhatsAppMonitorItemDto>> ObtenerMonitoreoGlobalAsync(int idUsuarioSolicitante = 0)
        {
            DataTable dt = _dWhatsApp.ObtenerUsuariosParaMonitoreoWhatsApp(idUsuarioSolicitante);
            var lista = new List<WhatsAppMonitorItemDto>();

            if (dt != null && dt.Rows.Count > 0)
            {
                foreach (DataRow row in dt.Rows)
                {
                    int idUsuario = Convert.ToInt32(row["IdUsuario"]);
                    string? urlServidor = row["UrlServidorWhatsApp"] == DBNull.Value ? null : row["UrlServidorWhatsApp"]?.ToString();
                    if (string.IsNullOrEmpty(urlServidor))
                    {
                        urlServidor = _dWhatsApp.ObtenerUrlServidorWhatsAppPorUsuario(idUsuario) ?? BridgeBaseUrl;
                    }

                    lista.Add(new WhatsAppMonitorItemDto
                    {
                        IdUsuario = idUsuario,
                        NombreUsuario = row["NombreCompleto"]?.ToString() ?? "",
                        Usuario = row["Usuario"]?.ToString() ?? "",
                        Rol = row["Rol"]?.ToString() ?? "",
                        IdTerritorio = row["IdTerritorio"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["IdTerritorio"]),
                        NombreTerritorio = row["NombreTerritorio"] == DBNull.Value ? "Sin territorio asignado" : row["NombreTerritorio"]?.ToString(),
                        TipoTerritorio = row["TipoTerritorio"] == DBNull.Value ? null : row["TipoTerritorio"]?.ToString(),
                        UrlServidorWhatsApp = urlServidor,
                        SessionId = $"u{idUsuario}_principal",
                        SessionName = $"{row["Usuario"]}_principal".ToUpper(),
                        Status = "DISCONNECTED",
                        PhoneE164 = null,
                        OutboundIp = null
                    });
                }
            }

            return lista;
        }

        private string NormalizarCelular(string? celularRaw)
        {
            if (string.IsNullOrWhiteSpace(celularRaw)) return string.Empty;
            string digits = System.Text.RegularExpressions.Regex.Replace(celularRaw, @"\D", "");
            if (digits.Length < 7) return string.Empty;

            if (digits.StartsWith("595")) return digits;
            if (digits.StartsWith("09") && digits.Length == 10) return "595" + digits.Substring(1);
            if (digits.Length == 9 && digits.StartsWith("9")) return "595" + digits;
            if (digits.Length == 8) return "595" + digits;

            return digits;
        }
    }
}

