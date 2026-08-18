using System;
using System.Data;
using Infrastructure;

namespace Application.Usuario
{
    /// <summary>
    /// Se lanza cuando un admin territorial intenta ver/modificar un usuario
    /// que no le pertenece (no fue creado por él). Ver §40 del manual de
    /// estándares (autorización por recurso / ownership).
    /// </summary>
    public class AccesoDenegadoException : Exception
    {
        public AccesoDenegadoException(string mensaje) : base(mensaje) { }
    }

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
            string? email,
            int idUsuarioCreador,
            int? idTerritorioCreador)
        {
            // Un admin territorial (idTerritorioCreador != null) solo puede crear
            // usuarios dentro de su propio territorio: se ignora lo que haya
            // mandado el request y se fuerza al territorio del creador. Un super
            // admin (idTerritorioCreador == null) mantiene libertad total,
            // incluyendo dejar el territorio en blanco.
            int? idTerritorioFinal = idTerritorioCreador ?? idTerritorio;

            string claveHash = BCrypt.Net.BCrypt.HashPassword(clave);
            return _data.Insertar(idRol, idTerritorioFinal, idUsuarioSupervisor, usuario, claveHash, nombreCompleto, ci, celular, email, idUsuarioCreador);
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
            bool activo,
            int idUsuarioCaller,
            int? idTerritorioCaller)
        {
            ValidarOwnership(idUsuario, idUsuarioCaller, idTerritorioCaller);

            int? idTerritorioFinal = idTerritorioCaller ?? idTerritorio;

            return _data.Actualizar(idUsuario, idRol, idTerritorioFinal, idUsuarioSupervisor, nombreCompleto, ci, celular, email, activo);
        }

        public DataTable CambiarClave(int idUsuario, string nuevaClave, int idUsuarioCaller, int? idTerritorioCaller)
        {
            ValidarOwnership(idUsuario, idUsuarioCaller, idTerritorioCaller);

            string claveHash = BCrypt.Net.BCrypt.HashPassword(nuevaClave);
            return _data.CambiarClave(idUsuario, claveHash);
        }

        public DataTable EliminarLogico(int idUsuario, int idUsuarioCaller, int? idTerritorioCaller)
        {
            ValidarOwnership(idUsuario, idUsuarioCaller, idTerritorioCaller);

            return _data.EliminarLogico(idUsuario);
        }

        public DataTable Listar(int? idRol, int? idTerritorio, int? idUsuarioSupervisor, bool soloActivos, int idUsuarioCaller, int? idTerritorioCaller)
        {
            // Super admin (sin territorio) -> sin filtro, ve todo.
            // Admin territorial -> solo lo que él mismo creó.
            int? idUsuarioCreateFiltro = idTerritorioCaller.HasValue ? idUsuarioCaller : (int?)null;

            return _data.Listar(idRol, idTerritorio, idUsuarioSupervisor, soloActivos, idUsuarioCreateFiltro);
        }

        public DataTable ObtenerPorId(int idUsuario, int idUsuarioCaller, int? idTerritorioCaller)
        {
            var ds = _data.ObtenerPorId(idUsuario);
            ValidarOwnership(ds, idUsuarioCaller, idTerritorioCaller);
            return ds;
        }

        private void ValidarOwnership(int idUsuario, int idUsuarioCaller, int? idTerritorioCaller)
        {
            if (!idTerritorioCaller.HasValue)
            {
                return; // super admin: sin restricción
            }

            var ds = _data.ObtenerPorId(idUsuario);
            ValidarOwnership(ds, idUsuarioCaller, idTerritorioCaller);
        }

        private void ValidarOwnership(DataTable ds, int idUsuarioCaller, int? idTerritorioCaller)
        {
            if (!idTerritorioCaller.HasValue)
            {
                return; // super admin: sin restricción
            }

            if (ds == null || ds.Rows.Count == 0)
            {
                return; // "no encontrado" lo maneja el llamador
            }

            var row = ds.Rows[0];
            int? creador = row["IdUsuarioCreate"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["IdUsuarioCreate"]);

            if (creador != idUsuarioCaller)
            {
                throw new AccesoDenegadoException("No tiene permiso sobre este usuario.");
            }
        }
    }
}