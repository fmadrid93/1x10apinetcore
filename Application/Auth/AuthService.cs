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

            string token = _jwtTokenService.GenerateToken(idUsuario, usuarioDb, rol);

            var dato = new
            {
                IdUsuario = idUsuario,
                Usuario = usuarioDb,
                NombreCompleto = row["NombreCompleto"]?.ToString(),
                IdRol = Convert.ToInt32(row["IdRol"]),
                Rol = rol,
                IdTerritorio = row["IdTerritorio"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["IdTerritorio"]),
                Territorio = row["Territorio"] == DBNull.Value ? null : row["Territorio"]?.ToString(),
                IdUsuarioSupervisor = row["IdUsuarioSupervisor"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["IdUsuarioSupervisor"]),
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