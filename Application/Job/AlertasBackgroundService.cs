using Application.Notificacion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Jobs
{
    public class AlertasBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AlertasBackgroundService> _logger;

        public AlertasBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<AlertasBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Servicio de alertas iniciado");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var alertaService = scope.ServiceProvider.GetRequiredService<Application.Alerta.AlertaService>();
                    var hora = DateTime.Now.Hour;

                    if (hora >= 6 && hora <= 20)
                    {
                        alertaService.GenerarSinCarga();
                        alertaService.GenerarBajoMeta();
                        alertaService.GenerarSinReporteDiaD();
                        alertaService.GenerarAvanceLento();
                    }

                    _logger.LogInformation("Alertas generadas correctamente: {hora}", DateTime.Now);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generando alertas automáticas");
                }

                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
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