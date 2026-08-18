using System.Data;
using Infrastructure;

namespace Application.Notificacion
{
    public class NotificacionService
    {
        private readonly DNotificacion _data = new DNotificacion();

        public DataTable RegistrarToken(int idUsuario, string token, string? plataforma)
        {
            return _data.RegistrarToken(idUsuario, token, plataforma);
        }

        public DataTable ObtenerTokenActivoPorUsuario(int idUsuario)
        {
            return _data.ObtenerTokenActivoPorUsuario(idUsuario);
        }

        public string? ObtenerTokenActivoValor(int idUsuario)
        {
            var ds = _data.ObtenerTokenActivoPorUsuario(idUsuario);

            if (ds.Rows.Count == 0) return null;

            return ds.Rows[0]["Token"]?.ToString();
        }
    }
}