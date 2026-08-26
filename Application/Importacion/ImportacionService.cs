using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using Domain;
using Dtos.Importacion;
using Infrastructure;

namespace Application.Importacion
{
    public class ImportacionService
    {
        private readonly DUsuario _dUsuario = new DUsuario();
        private readonly DPersonaMovilizada _dPersona = new DPersonaMovilizada();
        private readonly DRecinto _dRecinto = new DRecinto();
        private readonly DTerritorio _dTerritorio = new DTerritorio();
        private readonly Seguridad _seguridad = new Seguridad();

        public ImportacionMasivaResultadoDto ProcesarImportacionMasiva(
            ImportacionMasivaRequest request,
            int idUsuarioAdmin,
            int? idTerritorioAdmin)
        {
            var resultado = new ImportacionMasivaResultadoDto
            {
                TotalFilas = request.Filas?.Count ?? 0
            };

            if (request.Filas == null || request.Filas.Count == 0)
            {
                resultado.Errores.Add("No se enviaron registros para importar.");
                return resultado;
            }

            // 1. Cargar catálogo de Recintos en memoria para búsqueda ultra-rápida y normalizada
            var dtRecintos = _dRecinto.Listar();
            var dictRecintos = new Dictionary<string, (string IdRecinto, string Recinto)>(StringComparer.OrdinalIgnoreCase);
            if (dtRecintos != null && dtRecintos.Rows.Count > 0)
            {
                foreach (DataRow row in dtRecintos.Rows)
                {
                    string idRec = row["IdRecinto"]?.ToString() ?? "";
                    string recNombre = row["Recinto"]?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(recNombre))
                    {
                        string normal = NormalizarTexto(recNombre);
                        if (!dictRecintos.ContainsKey(normal))
                        {
                            dictRecintos[normal] = (idRec, recNombre);
                        }
                    }
                }
            }

            // 2. Cargar Usuarios existentes (para no duplicar Gerentes ni Movilizadores)
            var dtUsuarios = _dUsuario.Listar(null, null, null, false, null, null, null);
            var cacheGerentesPorUsuario = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var cacheGerentesPorNombre = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var cacheMovilizadoresPorUsuario = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var cacheMovilizadoresPorNombre = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            if (dtUsuarios != null && dtUsuarios.Rows.Count > 0)
            {
                foreach (DataRow row in dtUsuarios.Rows)
                {
                    int idU = Convert.ToInt32(row["IdUsuario"]);
                    string u = row["Usuario"]?.ToString()?.Trim() ?? "";
                    string nom = NormalizarTexto(row["NombreCompleto"]?.ToString() ?? "");
                    int idRol = Convert.ToInt32(row["IdRol"]);

                    if (idRol == 2) // GERENTE
                    {
                        if (!string.IsNullOrEmpty(u)) cacheGerentesPorUsuario[u] = idU;
                        if (!string.IsNullOrEmpty(nom)) cacheGerentesPorNombre[nom] = idU;
                    }
                    else if (idRol == 3) // MOVILIZADOR
                    {
                        if (!string.IsNullOrEmpty(u)) cacheMovilizadoresPorUsuario[u] = idU;
                        if (!string.IsNullOrEmpty(nom)) cacheMovilizadoresPorNombre[nom] = idU;
                    }
                }
            }

            // Clave Hash por defecto para nuevos usuarios creados
            string claveHash = _seguridad.GeneraClaveSHA1(request.ClavePorDefecto ?? "123456");

            // 3. Procesar fila por fila
            int indexFila = 0;
            foreach (var fila in request.Filas)
            {
                indexFila++;
                try
                {
                    int? idTerritorioFinal = fila.IdTerritorio ?? idTerritorioAdmin ?? request.IdTerritorioPorDefecto;

                    // A) Resolver Recinto por Nombre / Id
                    string? idRecintoFinal = fila.IdRecinto;
                    string? recintoNombreFinal = fila.NombreRecinto;

                    if (!string.IsNullOrWhiteSpace(fila.NombreRecinto))
                    {
                        string normalRec = NormalizarTexto(fila.NombreRecinto);
                        if (dictRecintos.TryGetValue(normalRec, out var rEncontrado))
                        {
                            idRecintoFinal = rEncontrado.IdRecinto;
                            recintoNombreFinal = rEncontrado.Recinto;
                            resultado.RecintosVinculados++;
                        }
                    }

                    // B) Resolver o Crear GERENTE
                    int? idGerenteFinal = null;
                    if (!string.IsNullOrWhiteSpace(fila.NombreGerente) || !string.IsNullOrWhiteSpace(fila.UsuarioGerente) || !string.IsNullOrWhiteSpace(fila.CiGerente))
                    {
                        string userGerente = GenerarUsuario(fila.UsuarioGerente, fila.NombreGerente, fila.CiGerente, "gerente");
                        string nomGerente = string.IsNullOrWhiteSpace(fila.NombreGerente) ? $"Gerente {fila.CiGerente}" : fila.NombreGerente.Trim();
                        string nomGerenteNormal = NormalizarTexto(nomGerente);

                        if (cacheGerentesPorUsuario.TryGetValue(userGerente, out int gId))
                        {
                            idGerenteFinal = gId;
                        }
                        else if (cacheGerentesPorNombre.TryGetValue(nomGerenteNormal, out int gId2))
                        {
                            idGerenteFinal = gId2;
                        }
                        else
                        {
                            // Crear nuevo Gerente
                            var dtNuevoG = _dUsuario.Insertar(
                                idRol: 2, // GERENTE
                                idTerritorio: idTerritorioFinal,
                                idUsuarioSupervisor: idUsuarioAdmin,
                                usuario: userGerente,
                                claveHash: claveHash,
                                nombreCompleto: nomGerente,
                                ci: fila.CiGerente,
                                celular: fila.CelularGerente,
                                email: null,
                                idUsuarioCreate: idUsuarioAdmin
                            );

                            if (dtNuevoG != null && dtNuevoG.Rows.Count > 0)
                            {
                                idGerenteFinal = Convert.ToInt32(dtNuevoG.Rows[0]["IdUsuario"]);
                                cacheGerentesPorUsuario[userGerente] = idGerenteFinal.Value;
                                cacheGerentesPorNombre[nomGerenteNormal] = idGerenteFinal.Value;
                                resultado.GerentesCreados++;
                            }
                        }
                    }

                    // C) Resolver o Crear MOVILIZADOR
                    int idMovilizadorFinal = idUsuarioAdmin; // Fallback al admin si no hay movilizador
                    if (!string.IsNullOrWhiteSpace(fila.NombreMovilizador) || !string.IsNullOrWhiteSpace(fila.UsuarioMovilizador) || !string.IsNullOrWhiteSpace(fila.CiMovilizador))
                    {
                        string userMovil = GenerarUsuario(fila.UsuarioMovilizador, fila.NombreMovilizador, fila.CiMovilizador, "movil");
                        string nomMovil = string.IsNullOrWhiteSpace(fila.NombreMovilizador) ? $"Movilizador {fila.CiMovilizador}" : fila.NombreMovilizador.Trim();
                        string nomMovilNormal = NormalizarTexto(nomMovil);

                        if (cacheMovilizadoresPorUsuario.TryGetValue(userMovil, out int mId))
                        {
                            idMovilizadorFinal = mId;
                        }
                        else if (cacheMovilizadoresPorNombre.TryGetValue(nomMovilNormal, out int mId2))
                        {
                            idMovilizadorFinal = mId2;
                        }
                        else
                        {
                            // Crear nuevo Movilizador subordinado al Gerente
                            var dtNuevoM = _dUsuario.Insertar(
                                idRol: 3, // MOVILIZADOR
                                idTerritorio: idTerritorioFinal,
                                idUsuarioSupervisor: idGerenteFinal ?? idUsuarioAdmin,
                                usuario: userMovil,
                                claveHash: claveHash,
                                nombreCompleto: nomMovil,
                                ci: fila.CiMovilizador,
                                celular: fila.CelularMovilizador,
                                email: null,
                                idUsuarioCreate: idUsuarioAdmin
                            );

                            if (dtNuevoM != null && dtNuevoM.Rows.Count > 0)
                            {
                                idMovilizadorFinal = Convert.ToInt32(dtNuevoM.Rows[0]["IdUsuario"]);
                                cacheMovilizadoresPorUsuario[userMovil] = idMovilizadorFinal;
                                cacheMovilizadoresPorNombre[nomMovilNormal] = idMovilizadorFinal;
                                resultado.MovilizadoresCreados++;
                            }
                        }
                    }

                    // D) Registrar Votante (Persona Movilizada)
                    if (!string.IsNullOrWhiteSpace(fila.VotanteNombres) || !string.IsNullOrWhiteSpace(fila.VotanteCI))
                    {
                        string nombresVotante = string.IsNullOrWhiteSpace(fila.VotanteNombres) ? "Votante" : fila.VotanteNombres.Trim();
                        string apellidosVotante = string.IsNullOrWhiteSpace(fila.VotanteApellidos) ? "." : fila.VotanteApellidos.Trim();

                        string? rangoEdadFinal = fila.VotanteRangoEdad;
                        if (string.IsNullOrEmpty(rangoEdadFinal) && !string.IsNullOrEmpty(fila.VotanteFechaNacimiento))
                        {
                            rangoEdadFinal = CalcularRangoEdad(fila.VotanteFechaNacimiento);
                        }
                        if (string.IsNullOrEmpty(rangoEdadFinal))
                        {
                            rangoEdadFinal = "26-35";
                        }

                        var dtInsertPersona = _dPersona.Insertar(
                            idUsuarioMovilizador: idMovilizadorFinal,
                            idTerritorio: idTerritorioFinal,
                            nombres: nombresVotante,
                            apellidos: apellidosVotante,
                            ci: fila.VotanteCI,
                            celular: fila.VotanteCelular,
                            direccionReferencia: fila.VotanteDireccion,
                            sexo: fila.VotanteSexo ?? "MASCULINO",
                            rangoEdad: rangoEdadFinal,
                            recintoVotacion: recintoNombreFinal,
                            idRecinto: idRecintoFinal,
                            requiereAyudaVotar: false,
                            nivelCompromiso: fila.VotanteNivelCompromiso ?? "MEDIO",
                            observaciones: "Importación Masiva Excel/JSON",
                            latitud: null,
                            longitud: null
                        );

                        if (dtInsertPersona != null && dtInsertPersona.Rows.Count > 0)
                        {
                            resultado.VotantesInsertados++;
                        }
                        else
                        {
                            resultado.VotantesDuplicadosOmitidos++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    resultado.Errores.Add($"Fila {indexFila} ({fila.VotanteNombres} {fila.VotanteApellidos}): {ex.Message}");
                }
            }

            return resultado;
        }

        private string NormalizarTexto(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
            string t = texto.Trim().ToLowerInvariant();
            t = Regex.Replace(t, @"[áàäâ]", "a");
            t = Regex.Replace(t, @"[éèëê]", "e");
            t = Regex.Replace(t, @"[íìïî]", "i");
            t = Regex.Replace(t, @"[óòöô]", "o");
            t = Regex.Replace(t, @"[úùüû]", "u");
            t = Regex.Replace(t, @"[^a-z0-9]", "");
            return t;
        }

        private string GenerarUsuario(string? usuario, string? nombre, string? ci, string prefijo)
        {
            if (!string.IsNullOrWhiteSpace(usuario))
            {
                return Regex.Replace(usuario.Trim().ToLowerInvariant(), @"[^a-z0-9_]", "");
            }
            if (!string.IsNullOrWhiteSpace(ci))
            {
                return $"{prefijo}_{ci.Trim()}";
            }
            if (!string.IsNullOrWhiteSpace(nombre))
            {
                string norm = NormalizarTexto(nombre);
                if (norm.Length > 12) norm = norm.Substring(0, 12);
                return $"{prefijo}_{norm}";
            }
            return $"{prefijo}_{Guid.NewGuid().ToString("N").Substring(0, 6)}";
        }

        private string CalcularRangoEdad(string fechaRaw)
        {
            if (DateTime.TryParse(fechaRaw, out var fecha))
            {
                var ahora = DateTime.Now;
                int edad = ahora.Year - fecha.Year;
                if (ahora.Month < fecha.Month || (ahora.Month == fecha.Month && ahora.Day < fecha.Day))
                {
                    edad--;
                }
                if (edad >= 18 && edad <= 25) return "18-25";
                if (edad >= 26 && edad <= 35) return "26-35";
                if (edad >= 36 && edad <= 45) return "36-45";
                if (edad >= 46 && edad <= 60) return "46-60";
                if (edad >= 61) return "61+";
            }
            return "26-35";
        }
    }
}
