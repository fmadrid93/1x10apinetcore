using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Application.Notificacion;
using Infrastructure;

namespace Application.Alerta
{
    public class AlertaService
    {
        private readonly DAlerta _data = new DAlerta();

        public DataTable GenerarMetaBaja()
        {
            return _data.GenerarMetaBaja();
        }

        public DataTable GenerarSinReporteDiaD()
        {
            return _data.GenerarSinReporteDiaD();
        }

        public DataTable ListarPorUsuario(int idUsuario)
        {
            return _data.ListarPorUsuario(idUsuario);
        }

        public DataTable Atender(int idAlerta)
        {
            return _data.Atender(idAlerta);
        }
        public DataTable GenerarSinCarga()
        {
            return _data.GenerarSinCarga();
        }

        public DataTable GenerarBajoMeta()
        {
            return _data.GenerarBajoMeta();
        }

        public DataTable ListarPorGerente(int idGerente)
        {
            return _data.ListarPorGerente(idGerente);
        }
        public DataTable GenerarAvanceLento()
        {
            return _data.GenerarAvanceLento();
        }
        public async Task EnviarPushAlertaAsync(int idUsuarioDestino, string titulo, string detalle, int idAlerta)
        {
            var notiService = new NotificacionService();
            var token = notiService.ObtenerTokenActivoValor(idUsuarioDestino);

            if (string.IsNullOrWhiteSpace(token))
                return;

            var push = new FirebasePushSender();
            push.Init();

            await push.EnviarAsync(
                token,
                titulo,
                detalle,
                new Dictionary<string, string>
                {
            { "type", "alerta" },
            { "idAlerta", idAlerta.ToString() },
            { "screen", "alertas" }
                }
            );
        }

    }
}