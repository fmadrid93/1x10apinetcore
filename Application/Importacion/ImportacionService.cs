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

            // 1. Cargar catálogo de Recintos en memoria para búsqueda normalizada
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

            // 2. Cargar Usuarios existentes (Gerentes y Movilizadores por CI, Usuario y Nombre)
            var dtUsuarios = _dUsuario.Listar(null, null, null, false, null, null, null);
            var cacheGerentesPorCI = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var cacheGerentesPorUsuario = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var cacheGerentesPorNombre = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            var cacheMovilizadoresPorCI = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var cacheMovilizadoresPorUsuario = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var cacheMovilizadoresPorNombre = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            var setUsuariosExistentes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (dtUsuarios != null && dtUsuarios.Rows.Count > 0)
            {
                foreach (DataRow row in dtUsuarios.Rows)
                {
                    int idU = Convert.ToInt32(row["IdUsuario"]);
                    string u = row["Usuario"]?.ToString()?.Trim() ?? "";
                    string ci = row["CI"]?.ToString()?.Trim() ?? "";
                    string nom = NormalizarTexto(row["NombreCompleto"]?.ToString() ?? "");
                    int idRol = Convert.ToInt32(row["IdRol"]);

                    if (!string.IsNullOrEmpty(u)) setUsuariosExistentes.Add(u);

                    if (idRol == 2) // GERENTE
                    {
                        if (!string.IsNullOrEmpty(ci)) cacheGerentesPorCI[ci] = idU;
                        if (!string.IsNullOrEmpty(u)) cacheGerentesPorUsuario[u] = idU;
                        if (!string.IsNullOrEmpty(nom)) cacheGerentesPorNombre[nom] = idU;
                    }
                    else if (idRol == 3) // MOVILIZADOR
                    {
                        if (!string.IsNullOrEmpty(ci)) cacheMovilizadoresPorCI[ci] = idU;
                        if (!string.IsNullOrEmpty(u)) cacheMovilizadoresPorUsuario[u] = idU;
                        if (!string.IsNullOrEmpty(nom)) cacheMovilizadoresPorNombre[nom] = idU;
                    }
                }
            }

            // 3. Cargar todos los CIs de votantes existentes para evitar duplicados
            var setVotantesCI = _dPersona.ListarTodosLosCIExistentes();

            // 4. Procesar fila por fila
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
                    string ciGerente = fila.CiGerente?.Trim() ?? "";
                    string nomGerente = string.IsNullOrWhiteSpace(fila.NombreGerente) ? (string.IsNullOrEmpty(ciGerente) ? "" : $"Gerente {ciGerente}") : fila.NombreGerente.Trim();
                    string nomGerenteNormal = NormalizarTexto(nomGerente);

                    if (!string.IsNullOrEmpty(ciGerente) || !string.IsNullOrEmpty(nomGerente) || !string.IsNullOrWhiteSpace(fila.UsuarioGerente))
                    {
                        if (!string.IsNullOrEmpty(ciGerente) && cacheGerentesPorCI.TryGetValue(ciGerente, out int gIdCi))
                        {
                            idGerenteFinal = gIdCi;
                            resultado.GerentesReutilizados++;
                        }
                        else if (!string.IsNullOrWhiteSpace(fila.UsuarioGerente) && cacheGerentesPorUsuario.TryGetValue(fila.UsuarioGerente.Trim(), out int gIdUser))
                        {
                            idGerenteFinal = gIdUser;
                            resultado.GerentesReutilizados++;
                        }
                        else if (!string.IsNullOrEmpty(nomGerenteNormal) && cacheGerentesPorNombre.TryGetValue(nomGerenteNormal, out int gIdNom))
                        {
                            idGerenteFinal = gIdNom;
                            resultado.GerentesReutilizados++;
                        }
                        else
                        {
                            // Generar usuario con regla: 1er caracter nombre + apellido (o 2 caracteres si existe)
                            string userGerente = GenerarUsuarioInteligente(fila.UsuarioGerente, nomGerente, ciGerente, "gerente", setUsuariosExistentes);

                            // Contraseña: El número de CI del Gerente (o clave por defecto si no tiene CI)
                            string claveGerenteTexto = !string.IsNullOrWhiteSpace(ciGerente) ? ciGerente : (request.ClavePorDefecto ?? "123456");
                            string claveGerenteHash = _seguridad.GeneraClaveSHA1(claveGerenteTexto);

                            var dtNuevoG = _dUsuario.Insertar(
                                idRol: 2, // GERENTE
                                idTerritorio: idTerritorioFinal,
                                idUsuarioSupervisor: idUsuarioAdmin,
                                usuario: userGerente,
                                claveHash: claveGerenteHash,
                                nombreCompleto: string.IsNullOrEmpty(nomGerente) ? $"Gerente {ciGerente}" : nomGerente,
                                ci: string.IsNullOrEmpty(ciGerente) ? null : ciGerente,
                                celular: fila.CelularGerente,
                                email: null,
                                idUsuarioCreate: idUsuarioAdmin
                            );

                            if (dtNuevoG != null && dtNuevoG.Rows.Count > 0)
                            {
                                idGerenteFinal = Convert.ToInt32(dtNuevoG.Rows[0]["IdUsuario"]);
                                if (!string.IsNullOrEmpty(ciGerente)) cacheGerentesPorCI[ciGerente] = idGerenteFinal.Value;
                                cacheGerentesPorUsuario[userGerente] = idGerenteFinal.Value;
                                if (!string.IsNullOrEmpty(nomGerenteNormal)) cacheGerentesPorNombre[nomGerenteNormal] = idGerenteFinal.Value;
                                resultado.GerentesCreados++;
                            }
                        }
                    }

                    // C) Resolver o Crear MOVILIZADOR
                    int idMovilizadorFinal = idUsuarioAdmin; // Fallback al admin si no hay movilizador
                    string ciMovil = fila.CiMovilizador?.Trim() ?? "";
                    string nomMovil = string.IsNullOrWhiteSpace(fila.NombreMovilizador) ? (string.IsNullOrEmpty(ciMovil) ? "" : $"Movilizador {ciMovil}") : fila.NombreMovilizador.Trim();
                    string nomMovilNormal = NormalizarTexto(nomMovil);

                    if (!string.IsNullOrEmpty(ciMovil) || !string.IsNullOrEmpty(nomMovil) || !string.IsNullOrWhiteSpace(fila.UsuarioMovilizador))
                    {
                        if (!string.IsNullOrEmpty(ciMovil) && cacheMovilizadoresPorCI.TryGetValue(ciMovil, out int mIdCi))
                        {
                            idMovilizadorFinal = mIdCi;
                            resultado.MovilizadoresReutilizados++;
                        }
                        else if (!string.IsNullOrWhiteSpace(fila.UsuarioMovilizador) && cacheMovilizadoresPorUsuario.TryGetValue(fila.UsuarioMovilizador.Trim(), out int mIdUser))
                        {
                            idMovilizadorFinal = mIdUser;
                            resultado.MovilizadoresReutilizados++;
                        }
                        else if (!string.IsNullOrEmpty(nomMovilNormal) && cacheMovilizadoresPorNombre.TryGetValue(nomMovilNormal, out int mIdNom))
                        {
                            idMovilizadorFinal = mIdNom;
                            resultado.MovilizadoresReutilizados++;
                        }
                        else
                        {
                            // Generar usuario con regla: 1er caracter nombre + apellido (o 2 caracteres si existe)
                            string userMovil = GenerarUsuarioInteligente(fila.UsuarioMovilizador, nomMovil, ciMovil, "movil", setUsuariosExistentes);

                            // Contraseña: El número de CI del Movilizador (o clave por defecto si no tiene CI)
                            string claveMovilTexto = !string.IsNullOrWhiteSpace(ciMovil) ? ciMovil : (request.ClavePorDefecto ?? "123456");
                            string claveMovilHash = _seguridad.GeneraClaveSHA1(claveMovilTexto);

                            var dtNuevoM = _dUsuario.Insertar(
                                idRol: 3, // MOVILIZADOR
                                idTerritorio: idTerritorioFinal,
                                idUsuarioSupervisor: idGerenteFinal ?? idUsuarioAdmin,
                                usuario: userMovil,
                                claveHash: claveMovilHash,
                                nombreCompleto: string.IsNullOrEmpty(nomMovil) ? $"Movilizador {ciMovil}" : nomMovil,
                                ci: string.IsNullOrEmpty(ciMovil) ? null : ciMovil,
                                celular: fila.CelularMovilizador,
                                email: null,
                                idUsuarioCreate: idUsuarioAdmin
                            );

                            if (dtNuevoM != null && dtNuevoM.Rows.Count > 0)
                            {
                                idMovilizadorFinal = Convert.ToInt32(dtNuevoM.Rows[0]["IdUsuario"]);
                                if (!string.IsNullOrEmpty(ciMovil)) cacheMovilizadoresPorCI[ciMovil] = idMovilizadorFinal;
                                cacheMovilizadoresPorUsuario[userMovil] = idMovilizadorFinal;
                                if (!string.IsNullOrEmpty(nomMovilNormal)) cacheMovilizadoresPorNombre[nomMovilNormal] = idMovilizadorFinal;
                                resultado.MovilizadoresCreados++;
                            }
                        }
                    }

                    // D) Validar y Registrar Votante (Persona Movilizada)
                    string ciVotante = fila.VotanteCI?.Trim() ?? "";
                    string nombresVotante = string.IsNullOrWhiteSpace(fila.VotanteNombres) ? "Votante" : fila.VotanteNombres.Trim();
                    string apellidosVotante = string.IsNullOrWhiteSpace(fila.VotanteApellidos) ? "." : fila.VotanteApellidos.Trim();

                    // VALIDACIÓN DE DUPLICADOS: Si el CI del votante ya existe en el sistema, omitir
                    if (!string.IsNullOrEmpty(ciVotante) && setVotantesCI.Contains(ciVotante))
                    {
                        resultado.VotantesDuplicadosOmitidos++;
                        resultado.Errores.Add($"Fila {indexFila}: El votante con CI {ciVotante} ({nombresVotante} {apellidosVotante}) ya está registrado. Se omitió para evitar duplicados.");
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(nombresVotante) || !string.IsNullOrWhiteSpace(ciVotante))
                    {
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
                            ci: string.IsNullOrEmpty(ciVotante) ? null : ciVotante,
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
                            if (!string.IsNullOrEmpty(ciVotante))
                            {
                                setVotantesCI.Add(ciVotante); // Agregar al set para no duplicar si se repite en el mismo archivo
                            }
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

        private string GenerarUsuarioInteligente(
            string? usuarioExplicito,
            string? nombreCompleto,
            string? ci,
            string prefijoFallback,
            HashSet<string> usuariosExistentes)
        {
            if (!string.IsNullOrWhiteSpace(usuarioExplicito))
            {
                string uLimpio = Regex.Replace(usuarioExplicito.Trim().ToLowerInvariant(), @"[^a-z0-9_]", "");
                if (!usuariosExistentes.Contains(uLimpio))
                {
                    usuariosExistentes.Add(uLimpio);
                    return uLimpio;
                }
            }

            if (!string.IsNullOrWhiteSpace(nombreCompleto))
            {
                var partes = nombreCompleto.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (partes.Length >= 1)
                {
                    string pNombre = NormalizarTexto(partes[0]);
                    string pApellido = partes.Length >= 2 ? NormalizarTexto(partes[1]) : pNombre;

                    if (!string.IsNullOrEmpty(pNombre) && !string.IsNullOrEmpty(pApellido))
                    {
                        // 1. Primer caracter del primer nombre + primer apellido (ej: Carlos Mendoza -> cmendoza)
                        string c1 = pNombre.Substring(0, 1) + (partes.Length >= 2 ? pApellido : "");
                        if (!usuariosExistentes.Contains(c1))
                        {
                            usuariosExistentes.Add(c1);
                            return c1;
                        }

                        // 2. Dos primeros caracteres del primer nombre + primer apellido (ej: Carlos Mendoza -> camendoza)
                        string c2 = (pNombre.Length >= 2 ? pNombre.Substring(0, 2) : pNombre) + (partes.Length >= 2 ? pApellido : "");
                        if (!usuariosExistentes.Contains(c2))
                        {
                            usuariosExistentes.Add(c2);
                            return c2;
                        }

                        // 3. Tres primeros caracteres del primer nombre + primer apellido (ej: Carlos Mendoza -> carmendoza)
                        string c3 = (pNombre.Length >= 3 ? pNombre.Substring(0, 3) : pNombre) + (partes.Length >= 2 ? pApellido : "");
                        if (!usuariosExistentes.Contains(c3))
                        {
                            usuariosExistentes.Add(c3);
                            return c3;
                        }

                        // 4. Nombre completo junto (ej: carlosmendoza)
                        string c4 = pNombre + (partes.Length >= 2 ? pApellido : "");
                        if (!usuariosExistentes.Contains(c4))
                        {
                            usuariosExistentes.Add(c4);
                            return c4;
                        }

                        // 5. Con sufijo de CI
                        if (!string.IsNullOrWhiteSpace(ci))
                        {
                            string cCi = $"{c1}_{Regex.Replace(ci.Trim().ToLowerInvariant(), @"[^a-z0-9]", "")}";
                            if (!usuariosExistentes.Contains(cCi))
                            {
                                usuariosExistentes.Add(cCi);
                                return cCi;
                            }
                        }

                        // 6. Con contador secuencial (cmendoza1, cmendoza2...)
                        for (int i = 1; i <= 99; i++)
                        {
                            string cNum = $"{c1}{i}";
                            if (!usuariosExistentes.Contains(cNum))
                            {
                                usuariosExistentes.Add(cNum);
                                return cNum;
                            }
                        }
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(ci))
            {
                string cCi = Regex.Replace(ci.Trim().ToLowerInvariant(), @"[^a-z0-9]", "");
                if (!usuariosExistentes.Contains(cCi))
                {
                    usuariosExistentes.Add(cCi);
                    return cCi;
                }
            }

            string fallback = $"{prefijoFallback}_{Guid.NewGuid().ToString("N").Substring(0, 6)}";
            usuariosExistentes.Add(fallback);
            return fallback;
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
