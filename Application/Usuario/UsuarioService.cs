using System.Data;
using Infrastructure;

namespace Application.Usuario
{
    public class UsuarioService
    {
        private readonly DUsuario _data = new DUsuario();

        public DataTable Insertar(
            int idRol,
            int? idTerritorio,
            int? idUsuarioSupervisor,
            string usuario,
            string clave,
            string nombreCompleto,
            string? ci,
            string? celular,
            string? email)
        {
            string claveHash = BCrypt.Net.BCrypt.HashPassword(clave);
            return _data.Insertar(idRol, idTerritorio, idUsuarioSupervisor, usuario, claveHash, nombreCompleto, ci, celular, email);
        }

        public DataTable Actualizar(
            int idUsuario,
            int idRol,
            int? idTerritorio,
            int? idUsuarioSupervisor,
            string nombreCompleto,
            string? ci,
            string? celular,
            string? email,
            bool activo)
        {
            return _data.Actualizar(idUsuario, idRol, idTerritorio, idUsuarioSupervisor, nombreCompleto, ci, celular, email, activo);
        }

        public DataTable CambiarClave(int idUsuario, string nuevaClave)
        {
            string claveHash = BCrypt.Net.BCrypt.HashPassword(nuevaClave);
            return _data.CambiarClave(idUsuario, claveHash);
        }

        public DataTable EliminarLogico(int idUsuario)
        {
            return _data.EliminarLogico(idUsuario);
        }

        public DataTable Listar(int? idRol, int? idTerritorio, int? idUsuarioSupervisor, bool soloActivos)
        {
            return _data.Listar(idRol, idTerritorio, idUsuarioSupervisor, soloActivos);
        }

        public DataTable ObtenerPorId(int idUsuario)
        {
            return _data.ObtenerPorId(idUsuario);
        }
    }
}