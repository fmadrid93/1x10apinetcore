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
        public string? NombreMovilizador { get; set; }
    }

    public class WhatsAppDifusionResultDto
    {
        public int TotalDestinatarios { get; set; }
        public int EncoladosCorrectamente { get; set; }
        public int Fallidos { get; set; }
        public List<string> SesionesUtilizadas { get; set; } = new List<string>();
        public string MensajeEstado { get; set; } = string.Empty;
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
