using System;
using System.Collections.Generic;

namespace Dtos.WhatsApp
{
    public class WhatsAppConfigDto
    {
        public bool HabilitarEnlaceQr { get; set; } = true;
        public string StorageMode { get; set; } = "DISK";
        public string SaasBaseUrl { get; set; } = string.Empty;
        public string OutboundIp { get; set; } = string.Empty;
    }

    public class WhatsAppSessionDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = "NEW"; // NEW | CONNECTING | CONNECTED | DISCONNECTED | LOGGED_OUT
        public string? PhoneE164 { get; set; }
        public string? QrDataUrl { get; set; }
        public string? OutboundIp { get; set; }
        public DateTime? ConnectedAt { get; set; }
        public DateTime? LastHeartbeatAt { get; set; }
    }

    public class WhatsAppCreateSessionRequest
    {
        public int IdUsuario { get; set; }
        public string? NombreAlias { get; set; }
        public string? ExpectedPhone { get; set; }
    }

    public class WhatsAppDifusionRequest
    {
        public int IdUsuario { get; set; }
        public string Rol { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public string? ImagenUrl { get; set; }
        public string? ImagenBase64 { get; set; }
        public List<string>? SessionIdsSeleccionadas { get; set; }
        public DateTime? FechaProgramada { get; set; }
        public bool EsBotEncuesta { get; set; } = false;
        public string? NombreCandidato { get; set; }
    }

    public class WhatsAppDestinatarioDto
    {
        public int IdPersonaMovilizada { get; set; }
        public int IdUsuarioMovilizador { get; set; }
        public string Nombres { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public string? CI { get; set; }
        public string Celular { get; set; } = string.Empty;
        public string? RecintoVotacion { get; set; }
        public string? EstadoDiaD { get; set; }
        public string EstadoApoyo { get; set; } = "PENDIENTE"; // PENDIENTE | CONSULTADO | APOYA | NO_APOYA
        public string? NombreMovilizador { get; set; }
    }

    public class WhatsAppDifusionResultDto
    {
        public int TotalDestinatarios { get; set; }
        public int EncoladosCorrectamente { get; set; }
        public int Fallidos { get; set; }
        public List<string> SesionesUtilizadas { get; set; } = new List<string>();
        public string MensajeEstado { get; set; } = string.Empty;
        public bool EsProgramado { get; set; } = false;
        public DateTime? FechaProgramada { get; set; }
    }

    public class WhatsAppBotConfigDto
    {
        public int IdBot { get; set; } = 1;
        public string Titulo { get; set; } = "Consulta de Intención de Voto";
        public string NombreCandidato { get; set; } = "nuestro candidato";
        public string PlantillaPregunta { get; set; } = "Hola {nombre}, ¿apoyarás a {candidato} en las próximas elecciones?\n\n1️⃣ Sí, totalmente\n2️⃣ Tal vez / Indeciso\n3️⃣ No\n\nPor favor responde con el número 1, 2 o 3.";
        
        public string Opcion1_Texto { get; set; } = "Sí, totalmente";
        public string Opcion1_EstadoApoyo { get; set; } = "APOYA";
        public string Opcion1_Respuesta { get; set; } = "¡Excelente {nombre}! Muchísimas gracias por tu respaldo a {candidato}. ¡Juntos vamos a ganar!";
        
        public string Opcion2_Texto { get; set; } = "Tal vez / Indeciso";
        public string Opcion2_EstadoApoyo { get; set; } = "CONSULTADO";
        public string Opcion2_Respuesta { get; set; } = "Gracias {nombre}. Te compartiremos nuestras principales propuestas para que conozcas a detalle el plan de trabajo de {candidato}.";
        
        public string Opcion3_Texto { get; set; } = "No";
        public string Opcion3_EstadoApoyo { get; set; } = "NO_APOYA";
        public string Opcion3_Respuesta { get; set; } = "Comprendemos tu postura, {nombre}. Agradecemos mucho tu sinceridad y tiempo. ¡Que tengas un excelente día!";
        
        public bool Activo { get; set; } = true;
    }

    public class WhatsAppBotRespuestaRequest
    {
        public string Celular { get; set; } = string.Empty;
        public string TextoRespuesta { get; set; } = string.Empty;
        public string? SessionId { get; set; }
    }

    public class WhatsAppBotRespuestaResultDto
    {
        public bool Reconocido { get; set; }
        public int? IdPersonaMovilizada { get; set; }
        public string? NombreVotante { get; set; }
        public string EstadoApoyoAsignado { get; set; } = "CONSULTADO"; // APOYA | NO_APOYA | CONSULTADO
        public string MensajeRespuesta { get; set; } = string.Empty;
    }

    public class WhatsAppMonitorItemDto
    {
        public int IdUsuario { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public int? IdTerritorio { get; set; }
        public string? NombreTerritorio { get; set; }
        public string? TipoTerritorio { get; set; }
        public string? UrlServidorWhatsApp { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public string SessionName { get; set; } = string.Empty;
        public string Status { get; set; } = "DISCONNECTED"; // CONNECTED | QR | DISCONNECTED
        public string? PhoneE164 { get; set; }
        public string? OutboundIp { get; set; }
    }
}
