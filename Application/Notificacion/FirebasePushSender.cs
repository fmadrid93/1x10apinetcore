using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Application.Notificacion
{
    //using FirebaseAdmin;
    //using Google.Apis.Auth.OAuth2;
    //using Microsoft.Extensions.Configuration;

    //public class FirebasePushSender
    //{
    //    private static bool _initialized = false;
    //    private readonly IConfiguration _configuration;

    //    public FirebasePushSender(IConfiguration configuration)
    //    {
    //        _configuration = configuration;
    //    }

    //    public void Init()
    //    {
    //        if (_initialized) return;

    //        var rutaRelativa = _configuration["Firebase:ServiceAccountPath"];
    //        var rutaCompleta = Path.Combine(AppContext.BaseDirectory, rutaRelativa!);

    //        FirebaseApp.Create(new AppOptions
    //        {
    //            Credential = GoogleCredential.FromFile(rutaCompleta)
    //        });

    //        _initialized = true;
    //    }
    //}
    public class FirebasePushSender
    {
        private static bool _initialized = false;

        public void Init()
        {
            if (_initialized) return;

            var ruta = Path.Combine(AppContext.BaseDirectory, "Firebase", "service-account.json");

            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile(ruta)
            });

            _initialized = true;
        }

        public async Task<string> EnviarAsync(
            string token,
            string titulo,
            string cuerpo,
            Dictionary<string, string>? data = null)
        {
            var message = new Message
            {
                Token = token,
                Notification = new Notification
                {
                    Title = titulo,
                    Body = cuerpo
                },
                Data = data ?? new Dictionary<string, string>()
            };

            return await FirebaseMessaging.DefaultInstance.SendAsync(message);
        }
    }
}