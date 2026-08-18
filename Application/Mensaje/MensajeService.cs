using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Application.Notificacion;
using Infrastructure;

namespace Application.Mensaje
{
    public class MensajeService
    {
        private readonly DMensaje _data = new DMensaje();

        public DataTable Enviar(int idUsuarioEmisor, int idUsuarioDestino, string titulo, string contenido)
        {
            // 1. Guardar en BD
            var ds = _data.Enviar(idUsuarioEmisor, idUsuarioDestino, titulo, contenido); ;

            // 2. Si salió bien, buscar token y mandar push
            if (ds != null && ds.Rows.Count>0 )
            {
                 EnviarPushMensajeAsync(idUsuarioDestino, titulo, contenido);
            }

            return ds;
        }

        public DataTable ListarBandeja(int idUsuarioDestino)
        {
            return _data.ListarBandeja(idUsuarioDestino);
        }

        public DataTable MarcarLeido(int idMensaje)
        {
            return _data.MarcarLeido(idMensaje);
        }


            public DataTable ListarRecibidos(int idUsuarioDestino)
            {
                return _data.ListarRecibidos(idUsuarioDestino);
            }

            public DataTable ListarEnviados(int idUsuarioEmisor)
            {
                return _data.ListarEnviados(idUsuarioEmisor);
            }
        public async Task EnviarPushMensajeAsync(int idUsuarioDestino, string titulo, string contenido)
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
                contenido,
                new Dictionary<string, string>
                {
            { "type", "mensaje" },
            { "screen", "bandeja" }
                }
            );
        }

    }
    }