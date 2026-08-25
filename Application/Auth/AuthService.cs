using System;
using System.Data;
using Infrastructure;
using Microsoft.Extensions.Configuration;

namespace Application.Auth
{
    public class AuthService
    {
        private readonly DUsuario _dUsuario = new DUsuario();
        private readonly JwtTokenService _jwtTokenService;

        public AuthService(IConfiguration configuration)
        {
            _jwtTokenService = new JwtTokenService(configuration);
        }

        public object Login(string usuario, string clave)
        {
            DataTable ds = _dUsuario.ObtenerParaLogin(usuario);

            if (ds == null || ds.Rows.Count == 0)
            {
                return new
                {
                    exito = 0,
                    dato = (object?)null,
                    status = "Usuario no encontrado"
                };
            }

            DataRow row = ds.Rows[0];

            bool activo = Convert.ToBoolean(row["Activo"]);
            if (!activo)
            {
                return new
                {
                    exito = 0,
                    dato = (object?)null,
                    status = "Usuario inactivo"
                };
            }

            string claveHash = row["ClaveHash"]?.ToString() ?? string.Empty;
           // string claveHash1 = BCrypt.Net.BCrypt.HashPassword(clave, workFactor: 11);
            bool claveValida = BCrypt.Net.BCrypt.Verify(clave, claveHash);

            if (!claveValida)
            {
                return new
                {
                    exito = 0,
                    dato = (object?)null,
                    status = "Credenciales inválidas"
                };
            }

            int idUsuario = Convert.ToInt32(row["IdUsuario"]);
            string usuarioDb = row["Usuario"]?.ToString() ?? "";
            string rol = row["Rol"]?.ToString() ?? "";
            int? idTerritorio = row["IdTerritorio"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["IdTerritorio"]);

            string token = _jwtTokenService.GenerateToken(idUsuario, usuarioDb, rol, idTerritorio);
            string? urlServidorWhatsApp = new DWhatsApp().ObtenerUrlServidorWhatsAppPorUsuario(idUsuario);

            string? municipio = null;
            string? zona = null;

            if (idTerritorio.HasValue)
            {
                try
                {
                    DataTable dtTerr = new DTerritorio().ObtenerPorId(idTerritorio.Value);
                    if (dtTerr != null && dtTerr.Rows.Count > 0)
                    {
                        DataRow rTerr = dtTerr.Rows[0];
                        string tipo = rTerr["TipoTerritorio"]?.ToString()?.ToUpper()?.Trim() ?? "";
                        string nombreTerr = rTerr["Nombre"]?.ToString()?.Trim() ?? "";
                        string nombrePadre = rTerr["NombrePadre"]?.ToString()?.Trim() ?? "";

                        if (tipo == "ZONA")
                        {
                            zona = nombreTerr;
                            municipio = !string.IsNullOrEmpty(nombrePadre) ? nombrePadre : null;
                        }
                        else if (tipo == "MUNICIPIO" || tipo == "CIUDAD" || tipo == "DISTRITO")
                        {
                            municipio = nombreTerr;
                        }
                        else
                        {
                            municipio = nombreTerr;
                        }
                    }
                }
                catch { }
            }

            var dato = new
            {
                IdUsuario = idUsuario,
                Usuario = usuarioDb,
                NombreCompleto = row["NombreCompleto"]?.ToString(),
                IdRol = Convert.ToInt32(row["IdRol"]),
                Rol = rol,
                IdTerritorio = idTerritorio,
                Territorio = row["Territorio"] == DBNull.Value ? null : row["Territorio"]?.ToString(),
                Municipio = municipio,
                Zona = zona,
                IdUsuarioSupervisor = row["IdUsuarioSupervisor"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["IdUsuarioSupervisor"]),
                UrlServidorWhatsApp = urlServidorWhatsApp,
                Token = token
            };


            return new
            {
                exito = 1,
                dato,
                status = "Login correcto"
            };
        }
    }
}