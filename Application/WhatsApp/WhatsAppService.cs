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
            var destinatarios = ObtenerDestinatariosNodo(request.IdUsuario, request.Rol);

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

            int exitosos = 0;
            int fallidos = 0;
            int sesionIndex = 0;
            var sesionesUsadas = new HashSet<string>();

            foreach (var dest in destinatarios)
            {
                string celular = NormalizarCelular(dest.Celular);
                if (string.IsNullOrEmpty(celular))
                {
                    fallidos++;
                    continue;
                }

                // Balanceo Round-Robin entre las sesiones conectadas del usuario
                var sesionActual = sesionesConectadas[sesionIndex % sesionesConectadas.Count];
                sesionIndex++;
                sesionesUsadas.Add(sesionActual.Id);

                // Personalizar mensaje con variables
                string textoFinal = request.Mensaje
                    .Replace("{nombre}", dest.Nombres)
                    .Replace("{apellido}", dest.Apellidos)
                    .Replace("{recinto}", dest.RecintoVotacion ?? "");

                try
                {
                    bool enviado = await EnviarMensajeBridgeAsync(serverUrl, sesionActual.Id, celular, textoFinal);
                    if (enviado) exitosos++;
                    else fallidos++;
                }
                catch
                {
                    fallidos++;
                }
            }

            return new WhatsAppDifusionResultDto
            {
                TotalDestinatarios = destinatarios.Count,
                EncoladosCorrectamente = exitosos,
                Fallidos = fallidos,
                SesionesUtilizadas = new List<string>(sesionesUsadas),
                MensajeEstado = $"Envío completado: {exitosos} mensaje(s) procesados a través de {sesionesUsadas.Count} cuenta(s) en servidor ({serverUrl})."
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

            if (digits.Length == 8)
            {
                digits = "591" + digits;
            }

            return digits;
        }
    }
}
