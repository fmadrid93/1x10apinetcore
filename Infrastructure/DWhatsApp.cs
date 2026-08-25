using System;
using System.Data;
using Infraestructure;
using Microsoft.Data.SqlClient;

namespace Infrastructure
{
    public class DWhatsApp : DbHelper
    {
        /// <summary>
        /// Obtiene todas las personas movilizadas correspondientes al nodo del usuario según su rol:
        /// - MOVILIZADOR: Solo las personas que él registró directamente.
        /// - GERENTE: Personas de todos los movilizadores que están bajo su supervisión.
        /// - ADMINISTRADOR: Todas las personas activas registradas en la campaña.
        /// </summary>
        public DataTable ObtenerPersonasPorNodo(int idUsuario, string rol)
        {
            string rolUpper = (rol ?? string.Empty).Trim().ToUpper();

            if (rolUpper == "MOVILIZADOR")
            {
                string sql = @"
                    SELECT 
                        pm.IdPersonaMovilizada,
                        pm.IdUsuarioMovilizador,
                        pm.Nombres,
                        pm.Apellidos,
                        pm.CI,
                        pm.Celular,
                        pm.RecintoVotacion,
                        pm.EstadoDiaD,
                        ISNULL(pm.EstadoApoyo, 'PENDIENTE') AS EstadoApoyo,
                        u.NombreCompleto AS NombreMovilizador
                    FROM PersonaMovilizada pm WITH (NOLOCK)
                    LEFT JOIN Usuario u WITH (NOLOCK) ON pm.IdUsuarioMovilizador = u.IdUsuario
                    WHERE pm.IdUsuarioMovilizador = @IdUsuario 
                      AND (pm.Activo IS NULL OR pm.Activo = 1)
                      AND pm.Celular IS NOT NULL 
                      AND LTRIM(RTRIM(pm.Celular)) <> ''
                    ORDER BY pm.Apellidos, pm.Nombres";

                return EjecutarSQLDirecto(sql, new SqlParameter("@IdUsuario", SqlDbType.Int) { Value = idUsuario });
            }
            else if (rolUpper == "GERENTE")
            {
                string sql = @"
                    SELECT 
                        pm.IdPersonaMovilizada,
                        pm.IdUsuarioMovilizador,
                        pm.Nombres,
                        pm.Apellidos,
                        pm.CI,
                        pm.Celular,
                        pm.RecintoVotacion,
                        pm.EstadoDiaD,
                        ISNULL(pm.EstadoApoyo, 'PENDIENTE') AS EstadoApoyo,
                        u.NombreCompleto AS NombreMovilizador
                    FROM PersonaMovilizada pm WITH (NOLOCK)
                    INNER JOIN Usuario u WITH (NOLOCK) ON pm.IdUsuarioMovilizador = u.IdUsuario
                    WHERE (u.IdUsuarioSupervisor = @IdUsuario OR pm.IdUsuarioMovilizador = @IdUsuario)
                      AND (pm.Activo IS NULL OR pm.Activo = 1)
                      AND pm.Celular IS NOT NULL 
                      AND LTRIM(RTRIM(pm.Celular)) <> ''
                    ORDER BY pm.Apellidos, pm.Nombres";

                return EjecutarSQLDirecto(sql, new SqlParameter("@IdUsuario", SqlDbType.Int) { Value = idUsuario });
            }
            else
            {
                // ADMINISTRADOR u otros roles directivos (todo el ámbito)
                string sql = @"
                    SELECT 
                        pm.IdPersonaMovilizada,
                        pm.IdUsuarioMovilizador,
                        pm.Nombres,
                        pm.Apellidos,
                        pm.CI,
                        pm.Celular,
                        pm.RecintoVotacion,
                        pm.EstadoDiaD,
                        ISNULL(pm.EstadoApoyo, 'PENDIENTE') AS EstadoApoyo,
                        u.NombreCompleto AS NombreMovilizador
                    FROM PersonaMovilizada pm WITH (NOLOCK)
                    LEFT JOIN Usuario u WITH (NOLOCK) ON pm.IdUsuarioMovilizador = u.IdUsuario
                    WHERE (pm.Activo IS NULL OR pm.Activo = 1)
                      AND pm.Celular IS NOT NULL 
                      AND LTRIM(RTRIM(pm.Celular)) <> ''
                    ORDER BY pm.Apellidos, pm.Nombres";

                return EjecutarSQLDirecto(sql);
            }
        }

        public string? ObtenerUrlServidorWhatsAppPorUsuario(int idUsuario)
        {
            string sql = @"
                WITH ArbolTerritorio AS (
                    SELECT 
                        t.IdTerritorio,
                        t.IdTerritorioPadre,
                        t.UrlServidorWhatsApp,
                        1 AS Nivel
                    FROM Usuario u WITH (NOLOCK)
                    LEFT JOIN Usuario sup WITH (NOLOCK) ON u.IdUsuarioSupervisor = sup.IdUsuario
                    INNER JOIN Territorio t WITH (NOLOCK) ON (
                        u.IdTerritorio = t.IdTerritorio 
                        OR (u.IdTerritorio IS NULL AND sup.IdTerritorio = t.IdTerritorio)
                    )
                    WHERE u.IdUsuario = @IdUsuario

                    UNION ALL

                    SELECT 
                        tp.IdTerritorio,
                        tp.IdTerritorioPadre,
                        tp.UrlServidorWhatsApp,
                        a.Nivel + 1
                    FROM Territorio tp WITH (NOLOCK)
                    INNER JOIN ArbolTerritorio a ON tp.IdTerritorio = a.IdTerritorioPadre
                )
                SELECT TOP 1 UrlServidorWhatsApp 
                FROM ArbolTerritorio 
                WHERE UrlServidorWhatsApp IS NOT NULL 
                  AND LTRIM(RTRIM(UrlServidorWhatsApp)) <> ''
                ORDER BY Nivel ASC";

            try
            {
                DataTable dt = EjecutarSQLDirecto(sql, new SqlParameter("@IdUsuario", SqlDbType.Int) { Value = idUsuario });
                if (dt != null && dt.Rows.Count > 0)
                {
                    var val = dt.Rows[0]["UrlServidorWhatsApp"]?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(val)) return val;
                }
            }
            catch { }

            return null;
        }

        public DataTable ObtenerUsuariosParaMonitoreoWhatsApp(int idUsuarioSolicitante)
        {
            string sql = @"
                WITH ArbolJerarquia AS (
                    SELECT 
                        u.IdUsuario,
                        u.IdUsuarioSupervisor,
                        u.IdTerritorio,
                        u.IdRol
                    FROM Usuario u WITH (NOLOCK)
                    WHERE u.IdUsuario = @IdUsuarioSolicitante

                    UNION ALL

                    SELECT 
                        h.IdUsuario,
                        h.IdUsuarioSupervisor,
                        h.IdTerritorio,
                        h.IdRol
                    FROM Usuario h WITH (NOLOCK)
                    INNER JOIN ArbolJerarquia a ON h.IdUsuarioSupervisor = a.IdUsuario
                ),
                TerritoriosValidos AS (
                    SELECT 
                        t.IdTerritorio,
                        t.IdTerritorioPadre
                    FROM Usuario u WITH (NOLOCK)
                    INNER JOIN Territorio t WITH (NOLOCK) ON u.IdTerritorio = t.IdTerritorio
                    WHERE u.IdUsuario = @IdUsuarioSolicitante

                    UNION ALL

                    SELECT 
                        h.IdTerritorio,
                        h.IdTerritorioPadre
                    FROM Territorio h WITH (NOLOCK)
                    INNER JOIN TerritoriosValidos tv ON h.IdTerritorioPadre = tv.IdTerritorio
                )
                SELECT DISTINCT
                    u.IdUsuario,
                    u.NombreCompleto,
                    u.Usuario,
                    r.Nombre AS Rol,
                    u.IdTerritorio,
                    ISNULL(t.Nombre, 'Sin Territorio Asignado') AS NombreTerritorio,
                    t.TipoTerritorio,
                    t.UrlServidorWhatsApp
                FROM Usuario u WITH (NOLOCK)
                INNER JOIN Rol r WITH (NOLOCK) ON u.IdRol = r.IdRol
                LEFT JOIN Territorio t WITH (NOLOCK) ON u.IdTerritorio = t.IdTerritorio
                WHERE u.Activo = 1
                  AND (
                    EXISTS (
                        SELECT 1 FROM Usuario sol WITH (NOLOCK) 
                        INNER JOIN Rol rsol WITH (NOLOCK) ON sol.IdRol = rsol.IdRol
                        WHERE sol.IdUsuario = @IdUsuarioSolicitante 
                          AND rsol.Nombre = 'ADMINISTRADOR'
                          AND sol.IdTerritorio IS NULL
                    )
                    OR @IdUsuarioSolicitante = 0
                    OR u.IdUsuario IN (SELECT IdUsuario FROM ArbolJerarquia)
                    OR u.IdTerritorio IN (SELECT IdTerritorio FROM TerritoriosValidos)
                  )
                ORDER BY NombreTerritorio, r.Nombre, u.NombreCompleto";

            return EjecutarSQLDirecto(sql, new SqlParameter("@IdUsuarioSolicitante", SqlDbType.Int) { Value = idUsuarioSolicitante });
        }

        public DataTable ActualizarCompromisoPorCelular(string celular, string estadoApoyo, string respuestaTexto)
        {
            string cleanDigits = System.Text.RegularExpressions.Regex.Replace(celular ?? "", @"\D", "");
            if (cleanDigits.Length > 8) cleanDigits = cleanDigits.Substring(cleanDigits.Length - 8);

            string nivel = estadoApoyo == "APOYA" ? "ALTO" : (estadoApoyo == "NO_APOYA" ? "BAJO" : "MEDIO");

            string sql = @"
                UPDATE PersonaMovilizada
                SET 
                    EstadoApoyo = @EstadoApoyo,
                    NivelCompromiso = @NivelCompromiso,
                    EstadoRegistro = CASE WHEN @EstadoApoyo = 'APOYA' THEN 'COMPROMETIDO' ELSE EstadoRegistro END,
                    Observacion = CASE 
                        WHEN Observacion IS NULL OR Observacion = '' THEN 'Bot WhatsApp: ' + @RespuestaTexto
                        ELSE Observacion + ' | Bot: ' + @RespuestaTexto
                    END,
                    FechaUpdate = GETDATE()
                OUTPUT 
                    inserted.IdPersonaMovilizada,
                    inserted.Nombres,
                    inserted.Apellidos,
                    inserted.EstadoApoyo,
                    inserted.NivelCompromiso,
                    inserted.Celular
                WHERE 
                    Celular = @Celular
                    OR Celular LIKE '%' + @CleanDigits";

            return EjecutarSQLDirecto(
                sql,
                new SqlParameter("@EstadoApoyo", SqlDbType.VarChar, 30) { Value = estadoApoyo },
                new SqlParameter("@NivelCompromiso", SqlDbType.VarChar, 50) { Value = nivel },
                new SqlParameter("@RespuestaTexto", SqlDbType.VarChar, 300) { Value = respuestaTexto },
                new SqlParameter("@Celular", SqlDbType.VarChar, 50) { Value = celular },
                new SqlParameter("@CleanDigits", SqlDbType.VarChar, 20) { Value = cleanDigits }
            );
        }

        public void MarcarComoConsultadosPorCelulares(string celularesSeparadosPorComa)
        {
            if (string.IsNullOrWhiteSpace(celularesSeparadosPorComa)) return;

            string sql = @"
                UPDATE PersonaMovilizada
                SET EstadoApoyo = 'CONSULTADO', FechaUpdate = GETDATE()
                WHERE EstadoApoyo = 'PENDIENTE'
                  AND Celular IN (SELECT value FROM STRING_SPLIT(@Celulares, ','))";

            try
            {
                EjecutarSQLDirecto(sql, new SqlParameter("@Celulares", SqlDbType.VarChar, -1) { Value = celularesSeparadosPorComa });
            }
            catch { }
        }

        public DataTable ObtenerBotConfiguracionBD()
        {
            string sql = @"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'WhatsAppBotConfig')
                BEGIN
                    CREATE TABLE dbo.WhatsAppBotConfig (
                        IdBot INT IDENTITY(1,1) PRIMARY KEY,
                        Titulo VARCHAR(150) NOT NULL DEFAULT 'Consulta de Intención de Voto',
                        NombreCandidato VARCHAR(150) NOT NULL DEFAULT 'nuestro candidato',
                        PlantillaPregunta VARCHAR(MAX) NOT NULL,
                        Opcion1_Texto VARCHAR(200) NOT NULL DEFAULT 'Sí, totalmente',
                        Opcion1_EstadoApoyo VARCHAR(30) NOT NULL DEFAULT 'APOYA',
                        Opcion1_Respuesta VARCHAR(MAX) NOT NULL,
                        Opcion2_Texto VARCHAR(200) NOT NULL DEFAULT 'Tal vez / Indeciso',
                        Opcion2_EstadoApoyo VARCHAR(30) NOT NULL DEFAULT 'CONSULTADO',
                        Opcion2_Respuesta VARCHAR(MAX) NOT NULL,
                        Opcion3_Texto VARCHAR(200) NOT NULL DEFAULT 'No',
                        Opcion3_EstadoApoyo VARCHAR(30) NOT NULL DEFAULT 'NO_APOYA',
                        Opcion3_Respuesta VARCHAR(MAX) NOT NULL,
                        Activo BIT NOT NULL DEFAULT 1,
                        FechaUpdate DATETIME DEFAULT GETDATE()
                    );

                    INSERT INTO dbo.WhatsAppBotConfig (
                        Titulo, NombreCandidato, PlantillaPregunta,
                        Opcion1_Texto, Opcion1_EstadoApoyo, Opcion1_Respuesta,
                        Opcion2_Texto, Opcion2_EstadoApoyo, Opcion2_Respuesta,
                        Opcion3_Texto, Opcion3_EstadoApoyo, Opcion3_Respuesta, Activo
                    ) VALUES (
                        'Consulta de Intención de Voto', 'nuestro candidato',
                        'Hola {nombre}, ¿apoyarás a {candidato} en las próximas elecciones?\n\n1️⃣ Sí, totalmente\n2️⃣ Tal vez / Indeciso\n3️⃣ No\n\nPor favor responde con el número 1, 2 o 3.',
                        'Sí, totalmente', 'APOYA', '¡Excelente {nombre}! Muchísimas gracias por tu respaldo a {candidato}. ¡Juntos vamos a ganar!',
                        'Tal vez / Indeciso', 'CONSULTADO', 'Gracias {nombre}. Te compartiremos nuestras principales propuestas para que conozcas a detalle el plan de trabajo de {candidato}.',
                        'No', 'NO_APOYA', 'Comprendemos tu postura, {nombre}. Agradecemos mucho tu sinceridad y tiempo. ¡Que tengas un excelente día!',
                        1
                    );
                END

                SELECT TOP 1 * FROM dbo.WhatsAppBotConfig WHERE Activo = 1 ORDER BY IdBot DESC";

            return EjecutarSQLDirecto(sql);
        }

        public DataTable GuardarBotConfiguracionBD(
            int idBot,
            string titulo,
            string nombreCandidato,
            string plantillaPregunta,
            string op1Texto,
            string op1Estado,
            string op1Resp,
            string op2Texto,
            string op2Estado,
            string op2Resp,
            string op3Texto,
            string op3Estado,
            string op3Resp,
            bool activo)
        {
            string sql = @"
                IF NOT EXISTS (SELECT 1 FROM dbo.WhatsAppBotConfig WHERE IdBot = @IdBot)
                BEGIN
                    INSERT INTO dbo.WhatsAppBotConfig (
                        Titulo, NombreCandidato, PlantillaPregunta,
                        Opcion1_Texto, Opcion1_EstadoApoyo, Opcion1_Respuesta,
                        Opcion2_Texto, Opcion2_EstadoApoyo, Opcion2_Respuesta,
                        Opcion3_Texto, Opcion3_EstadoApoyo, Opcion3_Respuesta, Activo, FechaUpdate
                    ) VALUES (
                        @Titulo, @NombreCandidato, @PlantillaPregunta,
                        @Opcion1_Texto, @Opcion1_EstadoApoyo, @Opcion1_Respuesta,
                        @Opcion2_Texto, @Opcion2_EstadoApoyo, @Opcion2_Respuesta,
                        @Opcion3_Texto, @Opcion3_EstadoApoyo, @Opcion3_Respuesta, @Activo, GETDATE()
                    );
                END
                ELSE
                BEGIN
                    UPDATE dbo.WhatsAppBotConfig
                    SET 
                        Titulo = @Titulo,
                        NombreCandidato = @NombreCandidato,
                        PlantillaPregunta = @PlantillaPregunta,
                        Opcion1_Texto = @Opcion1_Texto,
                        Opcion1_EstadoApoyo = @Opcion1_EstadoApoyo,
                        Opcion1_Respuesta = @Opcion1_Respuesta,
                        Opcion2_Texto = @Opcion2_Texto,
                        Opcion2_EstadoApoyo = @Opcion2_EstadoApoyo,
                        Opcion2_Respuesta = @Opcion2_Respuesta,
                        Opcion3_Texto = @Opcion3_Texto,
                        Opcion3_EstadoApoyo = @Opcion3_EstadoApoyo,
                        Opcion3_Respuesta = @Opcion3_Respuesta,
                        Activo = @Activo,
                        FechaUpdate = GETDATE()
                    WHERE IdBot = @IdBot;
                END

                SELECT TOP 1 * FROM dbo.WhatsAppBotConfig WHERE Activo = 1 ORDER BY IdBot DESC";

            return EjecutarSQLDirecto(
                sql,
                new SqlParameter("@IdBot", SqlDbType.Int) { Value = idBot },
                new SqlParameter("@Titulo", SqlDbType.VarChar, 150) { Value = titulo },
                new SqlParameter("@NombreCandidato", SqlDbType.VarChar, 150) { Value = nombreCandidato },
                new SqlParameter("@PlantillaPregunta", SqlDbType.VarChar, -1) { Value = plantillaPregunta },
                new SqlParameter("@Opcion1_Texto", SqlDbType.VarChar, 200) { Value = op1Texto },
                new SqlParameter("@Opcion1_EstadoApoyo", SqlDbType.VarChar, 30) { Value = op1Estado },
                new SqlParameter("@Opcion1_Respuesta", SqlDbType.VarChar, -1) { Value = op1Resp },
                new SqlParameter("@Opcion2_Texto", SqlDbType.VarChar, 200) { Value = op2Texto },
                new SqlParameter("@Opcion2_EstadoApoyo", SqlDbType.VarChar, 30) { Value = op2Estado },
                new SqlParameter("@Opcion2_Respuesta", SqlDbType.VarChar, -1) { Value = op2Resp },
                new SqlParameter("@Opcion3_Texto", SqlDbType.VarChar, 200) { Value = op3Texto },
                new SqlParameter("@Opcion3_EstadoApoyo", SqlDbType.VarChar, 30) { Value = op3Estado },
                new SqlParameter("@Opcion3_Respuesta", SqlDbType.VarChar, -1) { Value = op3Resp },
                new SqlParameter("@Activo", SqlDbType.Bit) { Value = activo }
            );
        }

        public DataTable GuardarCampanaProgramada(int idUsuario, string rol, string mensaje, DateTime fechaProgramada, string? sessionIdsJson, int totalDestinatarios)
        {
            string sql = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'WhatsAppCampanaProgramada')
                BEGIN
                    CREATE TABLE WhatsAppCampanaProgramada (
                        IdCampana INT IDENTITY(1,1) PRIMARY KEY,
                        IdUsuario INT NOT NULL,
                        Rol VARCHAR(50) NOT NULL,
                        Mensaje VARCHAR(MAX) NOT NULL,
                        FechaProgramada DATETIME NOT NULL,
                        FechaRegistro DATETIME DEFAULT GETDATE(),
                        SessionIdsJson VARCHAR(MAX) NULL,
                        TotalDestinatarios INT DEFAULT 0,
                        Estado VARCHAR(50) DEFAULT 'PROGRAMADO'
                    );
                END

                INSERT INTO WhatsAppCampanaProgramada (IdUsuario, Rol, Mensaje, FechaProgramada, SessionIdsJson, TotalDestinatarios, Estado)
                OUTPUT inserted.IdCampana, inserted.Estado, inserted.FechaProgramada
                VALUES (@IdUsuario, @Rol, @Mensaje, @FechaProgramada, @SessionIdsJson, @TotalDestinatarios, 'PROGRAMADO')";

            return EjecutarSQLDirecto(
                sql,
                new SqlParameter("@IdUsuario", SqlDbType.Int) { Value = idUsuario },
                new SqlParameter("@Rol", SqlDbType.VarChar, 50) { Value = rol },
                new SqlParameter("@Mensaje", SqlDbType.VarChar, -1) { Value = mensaje },
                new SqlParameter("@FechaProgramada", SqlDbType.DateTime) { Value = fechaProgramada },
                new SqlParameter("@SessionIdsJson", SqlDbType.VarChar, -1) { Value = (object?)sessionIdsJson ?? DBNull.Value },
                new SqlParameter("@TotalDestinatarios", SqlDbType.Int) { Value = totalDestinatarios }
            );
        }

        private DataTable EjecutarSQLDirecto(string sql, params SqlParameter[] parameters)
        {
            DataSet ds = new DataSet();
            try
            {
                abrirConexion();
                using (SqlCommand cmd = new SqlCommand(sql, obtenerConexion()))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandTimeout = 120;
                    if (parameters != null && parameters.Length > 0)
                    {
                        cmd.Parameters.AddRange(parameters);
                    }
                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(ds);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al consultar datos de WhatsApp: {ex.Message}");
            }
            finally
            {
                cerrarConexion();
            }

            return ds.Tables.Count > 0 ? ds.Tables[0] : new DataTable();
        }
    }
}
