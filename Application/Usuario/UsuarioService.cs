using System;
using System.Collections.Generic;
using System.Data;
using Infrastructure;

namespace Application.Usuario
{
    /// <summary>
    /// Se lanza cuando un admin/gerente territorial intenta ver/modificar un
    /// usuario que no le pertenece, o intenta una accion fuera de su
    /// alcance de rol (p.ej. un gerente creando otro gerente). Ver §40 del
    /// manual de estándares (autorización por recurso / ownership).
    /// </summary>
    public class AccesoDenegadoException : Exception
    {
        public AccesoDenegadoException(string mensaje) : base(mensaje) { }
    }

    public class UsuarioService
    {
        private const int RolGerente = 2;
        private const int RolMovilizador = 3;
        private const int RolVerificadorVoto = 1003;
        private const string NombreRolGerente = "GERENTE";

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
            int? idTerritorioCreador,
            string rolCreador,
            string? idRecinto = null)
        {
            int? idTerritorioFinal = idTerritorio;
            int? idUsuarioSupervisorFinal = idUsuarioSupervisor;

            if (rolCreador == NombreRolGerente)
            {
                if (idRol != RolMovilizador && idRol != RolVerificadorVoto)
                {
                    throw new AccesoDenegadoException("Un gerente solo puede registrar movilizadores o verificadores.");
                }

                // El gerente es siempre "el encargado": no elige territorio ni supervisor.
                idTerritorioFinal = idTerritorioCreador;
                idUsuarioSupervisorFinal = idRol == RolMovilizador ? idUsuarioCreador : (int?)null;
            }
            else if (idTerritorioCreador.HasValue)
            {
                // Admin territorial: territorio siempre fijo al suyo.
                idTerritorioFinal = idTerritorioCreador;

                if (idRol == RolGerente)
                {
                    // El gerente que registra el admin queda supervisado por el mismo admin.
                    idUsuarioSupervisorFinal = idUsuarioCreador;
                }
                else if (idRol == RolVerificadorVoto)
                {
                    // El verificador nunca tiene supervisor.
                    idUsuarioSupervisorFinal = null;
                }
                // Si idRol == RolMovilizador, el admin elige el gerente supervisor manualmente
                // (idUsuarioSupervisorFinal ya viene del request tal cual).
            }
            // Super admin (rolCreador == ADMINISTRADOR && idTerritorioCreador == null): libre, sin forzar nada.

            if (string.IsNullOrWhiteSpace(usuario))
            {
                usuario = GenerarUsuarioDisponible(nombreCompleto, ci);
            }
            else if (_data.ExisteUsuario(usuario.Trim()))
            {
                throw new Exception($"El usuario '{usuario.Trim()}' ya se encuentra registrado. Por favor elija un nombre de usuario diferente.");
            }

            if (string.IsNullOrWhiteSpace(clave))
            {
                clave = !string.IsNullOrWhiteSpace(ci) ? ci.Trim() : "123456";
            }

            if (idRol == RolMovilizador && !string.IsNullOrWhiteSpace(ci))
            {
                var existente = _data.ObtenerActivoPorCIyRol(ci.Trim(), RolMovilizador);
                if (existente != null && existente.Rows.Count > 0)
                {
                    var fila = existente.Rows[0];
                    int? supervisorExistente = fila["IdUsuarioSupervisor"] == DBNull.Value ? (int?)null : Convert.ToInt32(fila["IdUsuarioSupervisor"]);
                    if (supervisorExistente != idUsuarioSupervisorFinal)
                    {
                        string nombreExistente = fila["NombreCompleto"]?.ToString() ?? fila["Usuario"]?.ToString() ?? ci.Trim();
                        throw new Exception($"El CI '{ci.Trim()}' ya está registrado como movilizador ({nombreExistente}) bajo otro gerente. Un movilizador no puede pertenecer a más de un gerente.");
                    }
                }
            }

            string claveHash = BCrypt.Net.BCrypt.HashPassword(clave);
            return _data.Insertar(idRol, idTerritorioFinal, idUsuarioSupervisorFinal, usuario.Trim(), claveHash, nombreCompleto, ci, celular, email, idUsuarioCreador, idRecinto);
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
            int? idTerritorioCaller,
            string rolCaller,
            string? motivo,
            string? idRecinto = null)
        {
            ValidarOwnership(idUsuario, idUsuarioCaller, idTerritorioCaller, rolCaller);

            int? idTerritorioFinal = idTerritorio;
            int? idUsuarioSupervisorFinal = idUsuarioSupervisor;

            if (rolCaller == NombreRolGerente)
            {
                idTerritorioFinal = idTerritorio ?? idTerritorioCaller;
                idUsuarioSupervisorFinal = idRol == RolMovilizador ? idUsuarioCaller : (int?)null;
            }
            else
            {
                idTerritorioFinal = idTerritorio;

                if (idRol == RolGerente && idUsuarioSupervisor == null)
                {
                    idUsuarioSupervisorFinal = idUsuarioCaller;
                }
                else if (idRol == RolVerificadorVoto)
                {
                    idUsuarioSupervisorFinal = null;
                }
            }

            return _data.Actualizar(idUsuario, idRol, idTerritorioFinal, idUsuarioSupervisorFinal, nombreCompleto, ci, celular, email, activo, idUsuarioCaller, motivo, idRecinto);

        }

        public DataTable CambiarClave(int idUsuario, string nuevaClave, int idUsuarioCaller, int? idTerritorioCaller, string rolCaller, string? motivo)
        {
            ValidarOwnership(idUsuario, idUsuarioCaller, idTerritorioCaller, rolCaller);

            string claveHash = BCrypt.Net.BCrypt.HashPassword(nuevaClave);
            return _data.CambiarClave(idUsuario, claveHash, idUsuarioCaller, motivo);
        }

        public DataTable EliminarLogico(int idUsuario, int idUsuarioCaller, int? idTerritorioCaller, string rolCaller, string? motivo)
        {
            ValidarOwnership(idUsuario, idUsuarioCaller, idTerritorioCaller, rolCaller);

            return _data.EliminarLogico(idUsuario, idUsuarioCaller, motivo);
        }

        public DataTable Listar(int? idRol, int? idTerritorio, int? idUsuarioSupervisor, bool soloActivos, int idUsuarioCaller, int? idTerritorioCaller, string rolCaller)
        {
            var (idsCreador, idSupervisorPropio) = ResolverAlcance(idUsuarioCaller, idTerritorioCaller, rolCaller);

            return _data.Listar(idRol, idTerritorio, idUsuarioSupervisor, soloActivos, null, idsCreador, idSupervisorPropio);
        }

        public DataTable ObtenerPorId(int idUsuario, int idUsuarioCaller, int? idTerritorioCaller, string rolCaller)
        {
            var ds = _data.ObtenerPorId(idUsuario);
            ValidarOwnership(ds, idUsuarioCaller, idTerritorioCaller, rolCaller);
            return ds;
        }

        /// <summary>
        /// Calcula el alcance de visibilidad segun quien llama:
        ///   - Super admin: sin filtro.
        ///   - Admin territorial: lo que el creo + lo que crearon sus gerentes (jerarquico).
        ///   - Gerente: lo que el creo + los movilizadores que le asignaron como supervisor
        ///     (los verificadores no tienen supervisor, asi que ahi solo cuenta lo que el creo).
        /// </summary>
        private (string? idsCreador, int? idSupervisorPropio) ResolverAlcance(int idUsuarioCaller, int? idTerritorioCaller, string rolCaller)
        {
            if (rolCaller == NombreRolGerente)
            {
                return (idUsuarioCaller.ToString(), idUsuarioCaller);
            }

            if (!idTerritorioCaller.HasValue)
            {
                return (null, null); // super admin
            }

            var gerentesDs = _data.Listar(RolGerente, null, null, false, idUsuarioCaller, null, null);
            var ids = new List<int> { idUsuarioCaller };

            if (gerentesDs != null)
            {
                foreach (DataRow row in gerentesDs.Rows)
                {
                    ids.Add(Convert.ToInt32(row["IdUsuario"]));
                }
            }

            return (string.Join(",", ids), null);
        }

        private void ValidarOwnership(int idUsuario, int idUsuarioCaller, int? idTerritorioCaller, string rolCaller)
        {
            if (rolCaller != NombreRolGerente && !idTerritorioCaller.HasValue)
            {
                return; // super admin: sin restricción
            }

            var ds = _data.ObtenerPorId(idUsuario);
            ValidarOwnership(ds, idUsuarioCaller, idTerritorioCaller, rolCaller);
        }

        private void ValidarOwnership(DataTable ds, int idUsuarioCaller, int? idTerritorioCaller, string rolCaller)
        {
            if (rolCaller != NombreRolGerente && !idTerritorioCaller.HasValue)
            {
                return; // super admin: sin restricción
            }

            if (ds == null || ds.Rows.Count == 0)
            {
                return; // "no encontrado" lo maneja el llamador
            }

            var row = ds.Rows[0];
            int? creador = row["IdUsuarioCreate"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["IdUsuarioCreate"]);
            int? supervisor = row["IdUsuarioSupervisor"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["IdUsuarioSupervisor"]);

            bool permitido;

            if (rolCaller == NombreRolGerente)
            {
                // Gerente: lo que el creo, o lo que le asignaron como supervisor.
                permitido = creador == idUsuarioCaller || supervisor == idUsuarioCaller;
            }
            else
            {
                // Admin territorial: lo suyo directo, o lo que creo un gerente que el mismo registro.
                permitido = creador == idUsuarioCaller;

                if (!permitido && creador.HasValue)
                {
                    var creadorDs = _data.ObtenerPorId(creador.Value);
                    if (creadorDs != null && creadorDs.Rows.Count > 0)
                    {
                        var filaCreador = creadorDs.Rows[0];
                        int idRolCreador = Convert.ToInt32(filaCreador["IdRol"]);
                        int? creadorDelCreador = filaCreador["IdUsuarioCreate"] == DBNull.Value ? (int?)null : Convert.ToInt32(filaCreador["IdUsuarioCreate"]);

                        permitido = idRolCreador == RolGerente && creadorDelCreador == idUsuarioCaller;
                    }
                }
            }

            if (!permitido)
            {
                throw new AccesoDenegadoException("No tiene permiso sobre este usuario.");
            }
        }

        public string GenerarUsuarioDisponible(string nombreCompleto, string? ci = null)
        {
            if (string.IsNullOrWhiteSpace(nombreCompleto))
            {
                return !string.IsNullOrWhiteSpace(ci) ? $"u_{ci.Trim()}" : $"u_{DateTime.Now.Ticks % 100000}";
            }

            var partes = nombreCompleto.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length == 0)
            {
                return !string.IsNullOrWhiteSpace(ci) ? $"u_{ci.Trim()}" : $"u_{DateTime.Now.Ticks % 100000}";
            }

            string pNombre = NormalizarTexto(partes[0]);
            string pApellido = partes.Length >= 2 ? NormalizarTexto(partes[1]) : pNombre;

            if (string.IsNullOrEmpty(pNombre) || string.IsNullOrEmpty(pApellido))
            {
                return !string.IsNullOrWhiteSpace(ci) ? $"u_{ci.Trim()}" : $"u_{DateTime.Now.Ticks % 100000}";
            }

            // 1. Primer caracter del primer nombre + primer apellido (ej: Carlos Mendoza -> cmendoza)
            string c1 = pNombre.Substring(0, 1) + (partes.Length >= 2 ? pApellido : "");
            if (!_data.ExisteUsuario(c1)) return c1;

            // 2. Dos o más caracteres del primer nombre + primer apellido (ej: camendoza, carmendoza...)
            for (int len = 2; len <= pNombre.Length; len++)
            {
                string cLen = pNombre.Substring(0, len) + (partes.Length >= 2 ? pApellido : "");
                if (!_data.ExisteUsuario(cLen)) return cLen;
            }

            // 3. Con sufijo de CI
            if (!string.IsNullOrWhiteSpace(ci))
            {
                string cCi = $"{c1}_{System.Text.RegularExpressions.Regex.Replace(ci.Trim().ToLowerInvariant(), @"[^a-z0-9]", "")}";
                if (!_data.ExisteUsuario(cCi)) return cCi;
            }

            // 4. Con número secuencial (cmendoza1, cmendoza2...)
            for (int i = 1; i <= 999; i++)
            {
                string cNum = $"{c1}{i}";
                if (!_data.ExisteUsuario(cNum)) return cNum;
            }

            return $"{c1}_{DateTime.Now.Ticks % 10000}";
        }

        private string NormalizarTexto(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
            string t = texto.Trim().ToLowerInvariant();
            t = System.Text.RegularExpressions.Regex.Replace(t, @"[áàäâ]", "a");
            t = System.Text.RegularExpressions.Regex.Replace(t, @"[éèëê]", "e");
            t = System.Text.RegularExpressions.Regex.Replace(t, @"[íìïî]", "i");
            t = System.Text.RegularExpressions.Regex.Replace(t, @"[óòöô]", "o");
            t = System.Text.RegularExpressions.Regex.Replace(t, @"[úùüû]", "u");
            t = System.Text.RegularExpressions.Regex.Replace(t, @"[ñ]", "n");
            t = System.Text.RegularExpressions.Regex.Replace(t, @"[^a-z0-9]", "");
            return t;
        }
    }
}
