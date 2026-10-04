using Infrastructure;
using Microsoft.Data.SqlClient;
using System;
using System.Data;

public class DVotante : DbHelper
{
    private static bool _columnasPasoPCVerificadas = false;

    /// <summary>
    /// Auto-migración perezosa:
    /// agrega las columnas de "Pasó por el PC" y "Combustible" a TB_Votante la primera vez que
    /// se necesitan, sin requerir un script manual aparte.
    /// </summary>
    private void AsegurarColumnasPasoPorElPC()
    {
        if (_columnasPasoPCVerificadas) return;
        try
        {
            string sql = @"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('TB_Votante') AND name = 'PasoPorElPC')
                BEGIN
                    ALTER TABLE TB_Votante ADD PasoPorElPC BIT NULL, FechaPasoPorElPC DATETIME NULL, IdUsuarioMarcaPasoPC INT NULL;
                END
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('TB_Votante') AND name = 'Combustible')
                BEGIN
                    ALTER TABLE TB_Votante ADD Combustible BIT NULL, FechaCombustible DATETIME NULL, IdUsuarioMarcaCombustible INT NULL;
                END
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('TB_Votante') AND name = 'ObservacionPasoPorElPC')
                BEGIN
                    ALTER TABLE TB_Votante ADD ObservacionPasoPorElPC VARCHAR(500) NULL;
                END";
            EjecutarSQL(sql);
            _columnasPasoPCVerificadas = true;
        }
        catch
        {
            // Si falla por permisos, se sigue sin esta funcionalidad en vez de tumbar el resto.
        }
    }

    public DataTable ObtenerVotante(string ci)
    {
        AsegurarColumnasPasoPorElPC();
        return EjecutarPA(
            "PA_ObtenerVotante",
            new SqlParameter("@CI", SqlDbType.VarChar, 100) { Value = (object?)ci?.Trim() ?? DBNull.Value }
        );
    }

    public DataTable BuscarPadronGlobal(string texto, string? idRecinto = null, string? nroMesa = null, int? idTerritorio = null)
    {
        AsegurarColumnasPasoPorElPC();
        string t = (texto ?? "").Trim();

        try
        {
            var dt = EjecutarPA(
                "PA_VOTANTE_BUSCAR_PADRON_GLOBAL",
                new SqlParameter("@Texto", SqlDbType.VarChar, 100) { Value = t },
                new SqlParameter("@IdRecinto", SqlDbType.VarChar, 150) { Value = (object?)idRecinto ?? DBNull.Value },
                new SqlParameter("@NroMesa", SqlDbType.VarChar, 50) { Value = (object?)nroMesa ?? DBNull.Value },
                new SqlParameter("@IdTerritorio", SqlDbType.Int) { Value = (object?)idTerritorio ?? DBNull.Value }
            );

            if (dt != null && dt.Columns.Contains("ObservacionPasoPorElPC"))
            {
                return dt;
            }
        }
        catch
        {
        }

        try
        {
            var dt = EjecutarPA(
                "PA_VOTANTE_BUSCAR_PADRON_GLOBAL",
                new SqlParameter("@Texto", SqlDbType.VarChar, 100) { Value = t }
            );
            if (dt != null && dt.Columns.Contains("ObservacionPasoPorElPC"))
            {
                return dt;
            }
        }
        catch
        {
        }

        string sql = @"
            SELECT TOP 100
                v.IdVotante,
                v.Nombres,
                v.Apellidos,
                LTRIM(RTRIM(ISNULL(v.Nombres, ''))) + ' ' + LTRIM(RTRIM(ISNULL(v.Apellidos, ''))) AS NombreCompleto,
                v.CI,
                v.EstadoRegistro,
                ISNULL(v.EstadoDiaD, 'PENDIENTE') AS EstadoDiaD,
                v.FechaRegistro,
                v.FechaMarcaDiaD,
                ISNULL(v.IdUsuarioMarcaDiaD, 0) AS IdUsuarioMarcaDiaD,
                uMarcaVoto.NombreCompleto AS NombreUsuarioMarcaDiaD,
                uMarcaVoto.Usuario AS UsuarioMarcaDiaD,
                uSupVoto.NombreCompleto AS SupervisorUsuarioMarcaDiaD,
                v.Sexo,
                v.IdRecinto AS IdRecintoLegado,
                v.RecintoVotacion,
                COALESCE(v.Distrito, tMun.Nombre, '') AS Municipio,
                COALESCE(v.Distrito, tMun.Nombre, '') AS Distrito,
                COALESCE(v.Departamento, tDep.Nombre, '') AS Departamento,
                v.NroMesa,
                v.OrdenMesa AS NroOrden,
                v.IdRecintoOk AS IdRecinto,
                ISNULL(v.PasoPorElPC, 0) AS PasoPorElPC,
                v.FechaPasoPorElPC,
                v.IdUsuarioMarcaPasoPC,
                uMarcaPC.NombreCompleto AS NombreUsuarioMarcaPasoPC,
                uMarcaPC.Usuario AS UsuarioMarcaPasoPC,
                uSupPC.NombreCompleto AS SupervisorUsuarioMarcaPasoPC,
                v.ObservacionPasoPorElPC AS ObservacionPasoPorElPC,
                v.ObservacionPasoPorElPC AS ObservacionPC,
                v.ObservacionPasoPorElPC AS Observacion,
                CASE WHEN pm.IdPersonaMovilizada IS NOT NULL THEN 1 ELSE 0 END AS EsEstructura1x10,
                pm.IdPersonaMovilizada,
                pm.Celular AS CelularVotante,
                uMov.NombreCompleto AS Movilizador,
                uMov.Celular AS CelularMovilizador,
                uGer.NombreCompleto AS Gerente,
                uGer.Celular AS CelularGerente,
                0 AS PerteneceAOtroRecinto
            FROM dbo.TB_Votante v WITH (NOLOCK)
            LEFT JOIN dbo.TB_Recinto rRec WITH (NOLOCK) ON rRec.IdRecinto = TRY_CAST(v.IdRecintoOk AS UNIQUEIDENTIFIER)
            LEFT JOIN dbo.Territorio tMun WITH (NOLOCK) ON tMun.IdTerritorio = rRec.IdMunicipio
            LEFT JOIN dbo.Territorio tDep WITH (NOLOCK) ON tDep.IdTerritorio = tMun.IdTerritorioPadre
            LEFT JOIN dbo.PersonaMovilizada pm WITH (NOLOCK) ON pm.CI = v.CI AND (pm.Activo IS NULL OR pm.Activo = 1)
            LEFT JOIN dbo.Usuario uMov WITH (NOLOCK) ON uMov.IdUsuario = pm.IdUsuarioMovilizador
            LEFT JOIN dbo.Usuario uGer WITH (NOLOCK) ON uGer.IdUsuario = uMov.IdUsuarioSupervisor
            LEFT JOIN dbo.Usuario uMarcaVoto WITH (NOLOCK) ON uMarcaVoto.IdUsuario = v.IdUsuarioMarcaDiaD
            LEFT JOIN dbo.Usuario uSupVoto WITH (NOLOCK) ON uSupVoto.IdUsuario = uMarcaVoto.IdUsuarioSupervisor
            LEFT JOIN dbo.Usuario uMarcaPC WITH (NOLOCK) ON uMarcaPC.IdUsuario = v.IdUsuarioMarcaPasoPC
            LEFT JOIN dbo.Usuario uSupPC WITH (NOLOCK) ON uSupPC.IdUsuario = uMarcaPC.IdUsuarioSupervisor
            WHERE (
                @Texto = ''
                OR v.CI = @Texto
                OR v.CI LIKE @Texto + '%'
                OR v.Nombres LIKE '%' + @Texto + '%'
                OR v.Apellidos LIKE '%' + @Texto + '%'
            )
            AND (@IdRecinto IS NULL OR v.IdRecintoOk = @IdRecinto OR v.IdRecinto = @IdRecinto)
            AND (@NroMesa IS NULL OR v.NroMesa = @NroMesa)
            ORDER BY v.CI ASC;";

        return EjecutarSQL(
            sql,
            new SqlParameter("@Texto", SqlDbType.VarChar, 100) { Value = t },
            new SqlParameter("@IdRecinto", SqlDbType.VarChar, 150) { Value = (object?)idRecinto ?? DBNull.Value },
            new SqlParameter("@NroMesa", SqlDbType.VarChar, 50) { Value = (object?)nroMesa ?? DBNull.Value }
        );
    }

    public DataTable MarcarYaVoto(string idVotante, int idUsuarioMarca, string? observacion)
    {
        return EjecutarPA(
            "PA_VOTANTE_MARCAR_YA_VOTO",
            new SqlParameter("@IdVotante", SqlDbType.VarChar, 150) { Value = (object?)idVotante?.Trim() ?? DBNull.Value },
            new SqlParameter("@IdUsuarioMarca", SqlDbType.Int) { Value = idUsuarioMarca },
            new SqlParameter("@Observacion", SqlDbType.VarChar, 300) { Value = (object?)observacion ?? DBNull.Value }
        );
    }

    /// <summary>
    /// Marca "Pasó por el PC" (checkpoint distinto de "Ya Votó"): no pisa
    /// EstadoDiaD, queda en columnas propias para no mezclar los dos conceptos.
    /// </summary>
    public int MarcarPasoPorElPC(string idVotante, int idUsuarioMarca, string? observacion = null)
    {
        AsegurarColumnasPasoPorElPC();
        try
        {
            var dt = EjecutarPA(
                "PA_VOTANTE_MARCAR_PASO_PC",
                new SqlParameter("@IdVotante", SqlDbType.VarChar, 150) { Value = (object?)idVotante?.Trim() ?? DBNull.Value },
                new SqlParameter("@IdUsuarioMarca", SqlDbType.Int) { Value = idUsuarioMarca },
                new SqlParameter("@Observacion", SqlDbType.VarChar, 500) { Value = (object?)observacion ?? DBNull.Value }
            );

            if (dt != null && dt.Rows.Count > 0 && dt.Rows[0]["FilasAfectadas"] != DBNull.Value)
            {
                return Convert.ToInt32(dt.Rows[0]["FilasAfectadas"]);
            }
            return 0;
        }
        catch
        {
            string sql = @"
                DECLARE @IdGuid UNIQUEIDENTIFIER = TRY_CAST(LTRIM(RTRIM(@IdVotante)) AS UNIQUEIDENTIFIER);
                IF (@IdGuid IS NOT NULL)
                BEGIN
                    UPDATE TB_Votante
                    SET PasoPorElPC = 1,
                        FechaPasoPorElPC = GETDATE(),
                        IdUsuarioMarcaPasoPC = @IdUsuarioMarca,
                        ObservacionPasoPorElPC = CASE WHEN @Observacion IS NOT NULL AND LTRIM(RTRIM(@Observacion)) <> '' THEN LTRIM(RTRIM(@Observacion)) ELSE ObservacionPasoPorElPC END
                    WHERE IdVotante = @IdGuid;
                    SELECT @@ROWCOUNT AS FilasAfectadas;
                END
                ELSE
                BEGIN
                    UPDATE TB_Votante
                    SET PasoPorElPC = 1,
                        FechaPasoPorElPC = GETDATE(),
                        IdUsuarioMarcaPasoPC = @IdUsuarioMarca,
                        ObservacionPasoPorElPC = CASE WHEN @Observacion IS NOT NULL AND LTRIM(RTRIM(@Observacion)) <> '' THEN LTRIM(RTRIM(@Observacion)) ELSE ObservacionPasoPorElPC END
                    WHERE LTRIM(RTRIM(CAST(IdVotante AS VARCHAR(150)))) = LTRIM(RTRIM(@IdVotante));
                    SELECT @@ROWCOUNT AS FilasAfectadas;
                END";

            var dt = EjecutarSQL(
                sql,
                new SqlParameter("@IdVotante", SqlDbType.VarChar, 150) { Value = (object?)idVotante?.Trim() ?? DBNull.Value },
                new SqlParameter("@IdUsuarioMarca", SqlDbType.Int) { Value = idUsuarioMarca },
                new SqlParameter("@Observacion", SqlDbType.VarChar, 500) { Value = (object?)observacion ?? DBNull.Value }
            );

            if (dt != null && dt.Rows.Count > 0 && dt.Rows[0]["FilasAfectadas"] != DBNull.Value)
            {
                return Convert.ToInt32(dt.Rows[0]["FilasAfectadas"]);
            }
            return 0;
        }
    }

    /// <summary>
    /// Marca o desmarca entrega de Combustible para el votante (específico para Natalio / Checkpoint PC).
    /// </summary>
    public int MarcarCombustible(string idVotante, int idUsuarioMarca, bool recibioCombustible)
    {
        AsegurarColumnasPasoPorElPC();
        string sql = @"
            DECLARE @IdGuid UNIQUEIDENTIFIER = TRY_CAST(LTRIM(RTRIM(@IdVotante)) AS UNIQUEIDENTIFIER);
            IF (@IdGuid IS NOT NULL)
            BEGIN
                UPDATE TB_Votante
                SET Combustible = @Combustible,
                    FechaCombustible = CASE WHEN @Combustible = 1 THEN GETDATE() ELSE NULL END,
                    IdUsuarioMarcaCombustible = CASE WHEN @Combustible = 1 THEN @IdUsuarioMarca ELSE NULL END
                WHERE IdVotante = @IdGuid;
                SELECT @@ROWCOUNT AS FilasAfectadas;
            END
            ELSE
            BEGIN
                UPDATE TB_Votante
                SET Combustible = @Combustible,
                    FechaCombustible = CASE WHEN @Combustible = 1 THEN GETDATE() ELSE NULL END,
                    IdUsuarioMarcaCombustible = CASE WHEN @Combustible = 1 THEN @IdUsuarioMarca ELSE NULL END
                WHERE LTRIM(RTRIM(CAST(IdVotante AS VARCHAR(150)))) = LTRIM(RTRIM(@IdVotante));
                SELECT @@ROWCOUNT AS FilasAfectadas;
            END";

        var dt = EjecutarSQL(
            sql,
            new SqlParameter("@IdVotante", SqlDbType.VarChar, 150) { Value = (object?)idVotante?.Trim() ?? DBNull.Value },
            new SqlParameter("@IdUsuarioMarca", SqlDbType.Int) { Value = idUsuarioMarca },
            new SqlParameter("@Combustible", SqlDbType.Bit) { Value = recibioCombustible }
        );

        if (dt != null && dt.Rows.Count > 0 && dt.Rows[0]["FilasAfectadas"] != DBNull.Value)
        {
            return Convert.ToInt32(dt.Rows[0]["FilasAfectadas"]);
        }
        return 0;
    }

    /// <summary>
    /// Obtiene el CI por IdVotante usando búsqueda optimizada por índice.
    /// </summary>
    public string? ObtenerCIPorId(string idVotante)
    {
        string sql = @"
            DECLARE @IdGuid UNIQUEIDENTIFIER = TRY_CAST(LTRIM(RTRIM(@IdVotante)) AS UNIQUEIDENTIFIER);
            IF (@IdGuid IS NOT NULL)
            BEGIN
                SELECT TOP 1 CI
                FROM TB_Votante WITH (INDEX(IX_TB_Votante_IdVotante), NOLOCK)
                WHERE IdVotante = @IdGuid;
            END
            ELSE
            BEGIN
                SELECT TOP 1 CI
                FROM TB_Votante WITH (NOLOCK)
                WHERE LTRIM(RTRIM(CAST(IdVotante AS VARCHAR(150)))) = LTRIM(RTRIM(@IdVotante));
            END";

        var dt = EjecutarSQL(sql, new SqlParameter("@IdVotante", SqlDbType.VarChar, 150) { Value = (object?)idVotante?.Trim() ?? DBNull.Value });
        if (dt != null && dt.Rows.Count > 0 && dt.Rows[0]["CI"] != DBNull.Value)
        {
            string ci = dt.Rows[0]["CI"].ToString()?.Trim() ?? "";
            return string.IsNullOrEmpty(ci) ? null : ci;
        }
        return null;
    }

    public DataTable RecintoDiadConteos(string? idRecinto = null, int? idAdmin = null, string? nroMesa = null, int? idTerritorio = null)
    {
        return EjecutarPA(
            "PA_RECINTO_DIAD_MONITOREO",
            new SqlParameter("@Operacion", SqlDbType.VarChar, 30) { Value = "CONTEOS" },
            new SqlParameter("@IdRecinto", SqlDbType.VarChar, 150) { Value = (object?)idRecinto?.Trim() ?? DBNull.Value },
            new SqlParameter("@IdAdmin", SqlDbType.Int) { Value = (object?)idAdmin ?? DBNull.Value },
            new SqlParameter("@NroMesa", SqlDbType.VarChar, 50) { Value = (object?)nroMesa?.Trim() ?? DBNull.Value },
            new SqlParameter("@IdTerritorio", SqlDbType.Int) { Value = (object?)idTerritorio ?? DBNull.Value }
        );
    }

    public DataTable RecintoPadronFaltan(string? idRecinto = null, string? nroMesa = null, string? texto = null, int offset = 0, int limit = 100, int? idTerritorio = null)
    {
        return EjecutarPA(
            "PA_RECINTO_DIAD_MONITOREO",
            new SqlParameter("@Operacion", SqlDbType.VarChar, 30) { Value = "FALTAN_VOTAR_PADRON" },
            new SqlParameter("@IdRecinto", SqlDbType.VarChar, 150) { Value = (object?)idRecinto?.Trim() ?? DBNull.Value },
            new SqlParameter("@NroMesa", SqlDbType.VarChar, 50) { Value = (object?)nroMesa?.Trim() ?? DBNull.Value },
            new SqlParameter("@Texto", SqlDbType.VarChar, 100) { Value = (object?)texto?.Trim() ?? DBNull.Value },
            new SqlParameter("@Offset", SqlDbType.Int) { Value = offset },
            new SqlParameter("@Limit", SqlDbType.Int) { Value = limit },
            new SqlParameter("@IdTerritorio", SqlDbType.Int) { Value = (object?)idTerritorio ?? DBNull.Value }
        );
    }

    public DataTable RecintoVotaronNoRegistrados(string? idRecinto = null, string? nroMesa = null, string? texto = null, int offset = 0, int limit = 100, int? idTerritorio = null)
    {
        return EjecutarPA(
            "PA_RECINTO_DIAD_MONITOREO",
            new SqlParameter("@Operacion", SqlDbType.VarChar, 30) { Value = "VOTARON_NO_REGISTRADOS" },
            new SqlParameter("@IdRecinto", SqlDbType.VarChar, 150) { Value = (object?)idRecinto?.Trim() ?? DBNull.Value },
            new SqlParameter("@NroMesa", SqlDbType.VarChar, 50) { Value = (object?)nroMesa?.Trim() ?? DBNull.Value },
            new SqlParameter("@Texto", SqlDbType.VarChar, 100) { Value = (object?)texto?.Trim() ?? DBNull.Value },
            new SqlParameter("@Offset", SqlDbType.Int) { Value = offset },
            new SqlParameter("@Limit", SqlDbType.Int) { Value = limit },
            new SqlParameter("@IdTerritorio", SqlDbType.Int) { Value = (object?)idTerritorio ?? DBNull.Value }
        );
    }

    public DataTable RecintoRegistradosFaltan(string? idRecinto = null, int? idAdmin = null, string? nroMesa = null, string? texto = null, int offset = 0, int limit = 100, int? idTerritorio = null)
    {
        return EjecutarPA(
            "PA_RECINTO_DIAD_MONITOREO",
            new SqlParameter("@Operacion", SqlDbType.VarChar, 30) { Value = "REGISTRADOS_FALTAN" },
            new SqlParameter("@IdRecinto", SqlDbType.VarChar, 150) { Value = (object?)idRecinto?.Trim() ?? DBNull.Value },
            new SqlParameter("@IdAdmin", SqlDbType.Int) { Value = (object?)idAdmin ?? DBNull.Value },
            new SqlParameter("@NroMesa", SqlDbType.VarChar, 50) { Value = (object?)nroMesa?.Trim() ?? DBNull.Value },
            new SqlParameter("@Texto", SqlDbType.VarChar, 100) { Value = (object?)texto?.Trim() ?? DBNull.Value },
            new SqlParameter("@Offset", SqlDbType.Int) { Value = offset },
            new SqlParameter("@Limit", SqlDbType.Int) { Value = limit },
            new SqlParameter("@IdTerritorio", SqlDbType.Int) { Value = (object?)idTerritorio ?? DBNull.Value }
        );
    }

    public DataTable RecintoMesas(string? idRecinto = null, int? idTerritorio = null)
    {
        return EjecutarPA(
            "PA_RECINTO_DIAD_MONITOREO",
            new SqlParameter("@Operacion", SqlDbType.VarChar, 30) { Value = "MESAS" },
            new SqlParameter("@IdRecinto", SqlDbType.VarChar, 150) { Value = (object?)idRecinto?.Trim() ?? DBNull.Value },
            new SqlParameter("@IdTerritorio", SqlDbType.Int) { Value = (object?)idTerritorio ?? DBNull.Value }
        );
    }

    public DataTable ObtenerTop10(int? idTerritorio = null)
    {
        return BuscarPadronGlobal("", null, null, idTerritorio);
    }

    public DataTable VeedoresRendimientoResumen(int? idTerritorio = null, int? idAdmin = null, int? idGerente = null)
    {
        return EjecutarPA(
            "pa_veedor_rendimiento_resumen",
            new SqlParameter("@IdTerritorio", SqlDbType.Int) { Value = (object?)idTerritorio ?? DBNull.Value },
            new SqlParameter("@IdAdmin", SqlDbType.Int) { Value = (object?)idAdmin ?? DBNull.Value },
            new SqlParameter("@IdGerente", SqlDbType.Int) { Value = (object?)idGerente ?? DBNull.Value }
        );
    }

    public DataTable VotantesMarcadosListar(
        int? idUsuarioMarca = null,
        string? tipoMarca = null,
        int? idTerritorio = null,
        int? idAdmin = null,
        int? idGerente = null,
        int? idMovilizador = null,
        string? texto = null,
        int offset = 0,
        int limit = 1000)
    {
        AsegurarColumnasPasoPorElPC();
        try
        {
            var dt = EjecutarPA(
                "pa_veedor_votantes_marcados_listar",
                new SqlParameter("@IdUsuarioMarca", SqlDbType.Int) { Value = (object?)idUsuarioMarca ?? DBNull.Value },
                new SqlParameter("@TipoMarca", SqlDbType.VarChar, 20) { Value = (object?)tipoMarca ?? DBNull.Value },
                new SqlParameter("@IdTerritorio", SqlDbType.Int) { Value = (object?)idTerritorio ?? DBNull.Value },
                new SqlParameter("@IdAdmin", SqlDbType.Int) { Value = (object?)idAdmin ?? DBNull.Value },
                new SqlParameter("@IdGerente", SqlDbType.Int) { Value = (object?)idGerente ?? DBNull.Value },
                new SqlParameter("@IdMovilizador", SqlDbType.Int) { Value = (object?)idMovilizador ?? DBNull.Value },
                new SqlParameter("@Texto", SqlDbType.VarChar, 100) { Value = (object?)texto ?? DBNull.Value },
                new SqlParameter("@Offset", SqlDbType.Int) { Value = offset },
                new SqlParameter("@Limit", SqlDbType.Int) { Value = limit }
            );

            if (dt != null && dt.Columns.Contains("ObservacionPasoPorElPC"))
            {
                return dt;
            }
        }
        catch
        {
        }

        string sql = @"
                SELECT
                    v.IdVotante,
                    v.CI,
                    LTRIM(RTRIM(ISNULL(v.Nombres, ''))) + ' ' + LTRIM(RTRIM(ISNULL(v.Apellidos, ''))) AS NombreCompleto,
                    v.Nombres,
                    v.Apellidos,
                    v.RecintoVotacion,
                    v.NroMesa,
                    v.OrdenMesa AS NroOrden,
                    COALESCE(v.Distrito, tMun.Nombre, '') AS Distrito,
                    COALESCE(v.Distrito, tMun.Nombre, '') AS Municipio,
                    COALESCE(v.Departamento, tDep.Nombre, '') AS Departamento,
                    v.EstadoDiaD,
                    v.FechaMarcaDiaD,
                    v.IdUsuarioMarcaDiaD,
                    uMarcaVoto.NombreCompleto AS NombreUsuarioMarcaDiaD,
                    uMarcaVoto.Usuario AS UsuarioMarcaDiaD,
                    uSupVoto.NombreCompleto AS SupervisorUsuarioMarcaDiaD,
                    ISNULL(v.PasoPorElPC, 0) AS PasoPorElPC,
                    v.FechaPasoPorElPC,
                    v.IdUsuarioMarcaPasoPC,
                    uMarcaPC.NombreCompleto AS NombreUsuarioMarcaPasoPC,
                    uMarcaPC.Usuario AS UsuarioMarcaPasoPC,
                    uSupPC.NombreCompleto AS SupervisorUsuarioMarcaPasoPC,
                    v.ObservacionPasoPorElPC AS ObservacionPasoPorElPC,
                    v.ObservacionPasoPorElPC AS ObservacionPC,
                    v.ObservacionPasoPorElPC AS Observacion,
                    ISNULL(v.Combustible, 0) AS RecibioCombustible,
                    ISNULL(v.Combustible, 0) AS Combustible,
                    v.FechaCombustible,
                    v.IdUsuarioMarcaCombustible,
                    uMarcaComb.NombreCompleto AS NombreUsuarioMarcaCombustible,
                    uMarcaComb.Usuario AS UsuarioMarcaCombustible,
                    CASE WHEN pm.IdPersonaMovilizada IS NOT NULL THEN 1 ELSE 0 END AS EsEstructura1x10,
                    pm.IdPersonaMovilizada,
                    pm.Celular AS CelularVotante,
                    uMov.NombreCompleto AS Movilizador,
                    uMov.Celular AS CelularMovilizador,
                    uGer.NombreCompleto AS Gerente,
                    uGer.Celular AS CelularGerente
                FROM dbo.TB_Votante v WITH (NOLOCK)
                LEFT JOIN dbo.TB_Recinto rRec WITH (NOLOCK) ON rRec.IdRecinto = TRY_CAST(v.IdRecintoOk AS UNIQUEIDENTIFIER)
                LEFT JOIN dbo.Territorio tMun WITH (NOLOCK) ON tMun.IdTerritorio = rRec.IdMunicipio
                LEFT JOIN dbo.Territorio tDep WITH (NOLOCK) ON tDep.IdTerritorio = tMun.IdTerritorioPadre
                LEFT JOIN dbo.PersonaMovilizada pm WITH (NOLOCK) ON pm.CI = v.CI AND (pm.Activo IS NULL OR pm.Activo = 1)
                LEFT JOIN dbo.Usuario uMov WITH (NOLOCK) ON uMov.IdUsuario = pm.IdUsuarioMovilizador
                LEFT JOIN dbo.Usuario uGer WITH (NOLOCK) ON uGer.IdUsuario = uMov.IdUsuarioSupervisor
                LEFT JOIN dbo.Usuario uMarcaVoto WITH (NOLOCK) ON uMarcaVoto.IdUsuario = v.IdUsuarioMarcaDiaD
                LEFT JOIN dbo.Usuario uSupVoto WITH (NOLOCK) ON uSupVoto.IdUsuario = COALESCE(uMarcaVoto.IdUsuarioSupervisor, uMarcaVoto.IdUsuarioCreate)
                LEFT JOIN dbo.Usuario uMarcaPC WITH (NOLOCK) ON uMarcaPC.IdUsuario = v.IdUsuarioMarcaPasoPC
                LEFT JOIN dbo.Usuario uSupPC WITH (NOLOCK) ON uSupPC.IdUsuario = COALESCE(uMarcaPC.IdUsuarioSupervisor, uMarcaPC.IdUsuarioCreate)
                LEFT JOIN dbo.Usuario uMarcaComb WITH (NOLOCK) ON uMarcaComb.IdUsuario = v.IdUsuarioMarcaCombustible
                WHERE (
                    (ISNULL(v.PasoPorElPC, 0) = 1 OR v.EstadoDiaD = 'YA_VOTO')
                )
                AND (
                    @IdUsuarioMarca IS NULL 
                    OR v.IdUsuarioMarcaPasoPC = @IdUsuarioMarca 
                    OR v.IdUsuarioMarcaDiaD = @IdUsuarioMarca
                    OR v.IdUsuarioMarcaCombustible = @IdUsuarioMarca
                )
                AND (
                    @TipoMarca IS NULL OR @TipoMarca = 'TODOS'
                    OR (@TipoMarca = 'PC' AND ISNULL(v.PasoPorElPC, 0) = 1)
                    OR ((@TipoMarca = 'VOTO' OR @TipoMarca = 'YA_VOTO') AND v.EstadoDiaD = 'YA_VOTO')
                )
                AND (
                    @IdTerritorio IS NULL
                    OR @IdUsuarioMarca IS NOT NULL
                    OR rRec.IdMunicipio = @IdTerritorio
                    OR tMun.IdTerritorioPadre = @IdTerritorio
                )
                AND (
                    @IdAdmin IS NULL
                    OR @IdUsuarioMarca IS NOT NULL
                    OR uSupVoto.IdUsuarioSupervisor = @IdAdmin
                    OR uSupPC.IdUsuarioSupervisor = @IdAdmin
                    OR uMarcaVoto.IdUsuarioSupervisor = @IdAdmin
                    OR uMarcaPC.IdUsuarioSupervisor = @IdAdmin
                    OR uMarcaVoto.IdUsuarioCreate = @IdAdmin
                    OR uMarcaPC.IdUsuarioCreate = @IdAdmin
                    OR uMarcaVoto.IdUsuario = @IdAdmin
                    OR uMarcaPC.IdUsuario = @IdAdmin
                )
                AND (
                    @IdGerente IS NULL
                    OR @IdUsuarioMarca IS NOT NULL
                    OR uGer.IdUsuario = @IdGerente
                    OR uSupVoto.IdUsuario = @IdGerente
                    OR uSupPC.IdUsuario = @IdGerente
                    OR uMarcaVoto.IdUsuarioSupervisor = @IdGerente
                    OR uMarcaPC.IdUsuarioSupervisor = @IdGerente
                    OR uMarcaVoto.IdUsuario = @IdGerente
                    OR uMarcaPC.IdUsuario = @IdGerente
                )
                AND (
                    @IdMovilizador IS NULL
                    OR pm.IdUsuarioMovilizador = @IdMovilizador
                )
                AND (
                    @Texto IS NULL OR @Texto = ''
                    OR v.CI = @Texto
                    OR v.CI LIKE @Texto + '%'
                    OR v.Nombres LIKE '%' + @Texto + '%'
                    OR v.Apellidos LIKE '%' + @Texto + '%'
                    OR v.RecintoVotacion LIKE '%' + @Texto + '%'
                )
                ORDER BY 
                    COALESCE(v.FechaPasoPorElPC, v.FechaMarcaDiaD) DESC,
                    v.NroMesa ASC,
                    v.OrdenMesa ASC
                OFFSET @Offset ROWS
                FETCH NEXT @Limit ROWS ONLY
                OPTION (RECOMPILE);";

            return EjecutarSQL(
                sql,
                new SqlParameter("@IdUsuarioMarca", SqlDbType.Int) { Value = (object?)idUsuarioMarca ?? DBNull.Value },
                new SqlParameter("@TipoMarca", SqlDbType.VarChar, 20) { Value = (object?)tipoMarca ?? DBNull.Value },
                new SqlParameter("@IdTerritorio", SqlDbType.Int) { Value = (object?)idTerritorio ?? DBNull.Value },
                new SqlParameter("@IdAdmin", SqlDbType.Int) { Value = (object?)idAdmin ?? DBNull.Value },
                new SqlParameter("@IdGerente", SqlDbType.Int) { Value = (object?)idGerente ?? DBNull.Value },
                new SqlParameter("@IdMovilizador", SqlDbType.Int) { Value = (object?)idMovilizador ?? DBNull.Value },
                new SqlParameter("@Texto", SqlDbType.VarChar, 100) { Value = (object?)texto ?? DBNull.Value },
                new SqlParameter("@Offset", SqlDbType.Int) { Value = offset },
                new SqlParameter("@Limit", SqlDbType.Int) { Value = limit }
            );
    }

    /// <summary>
    /// Limpia las marcas del Día D (Votos, Gasolina/Combustible y Pasó por el PC)
    /// en TB_Votante y PersonaMovilizada para el SuperAdmin.
    /// </summary>
    public int SuperAdminLimpiarMarcasDiaD(bool limpiarVoto, bool limpiarGasolina, bool limpiarPasoPC, int? idTerritorio = null)
    {
        AsegurarColumnasPasoPorElPC();

        try
        {
            var dtPa = EjecutarPA(
                "PA_LIMPIAR_MARCAS_DIAD",
                new SqlParameter("@LimpiarVoto", SqlDbType.Bit) { Value = limpiarVoto },
                new SqlParameter("@LimpiarGasolina", SqlDbType.Bit) { Value = limpiarGasolina },
                new SqlParameter("@LimpiarPasoPC", SqlDbType.Bit) { Value = limpiarPasoPC },
                new SqlParameter("@IdTerritorio", SqlDbType.Int) { Value = (object?)idTerritorio ?? DBNull.Value }
            );

            if (dtPa != null && dtPa.Rows.Count > 0 && dtPa.Rows[0]["TotalAfectadas"] != DBNull.Value)
            {
                return Convert.ToInt32(dtPa.Rows[0]["TotalAfectadas"]);
            }
        }
        catch
        {
        }

        string sql = @"
            SET NOCOUNT OFF;
            DECLARE @TotalAfectadas INT = 0;

            -- 1. LIMPIAR VOTOS (TB_Votante y PersonaMovilizada)
            IF (@LimpiarVoto = 1)
            BEGIN
                -- TB_Votante
                UPDATE v
                SET v.EstadoDiaD = 'PENDIENTE',
                    v.FechaMarcaDiaD = NULL,
                    v.IdUsuarioMarcaDiaD = NULL,
                    v.ObservacionDiaD = NULL
                FROM dbo.TB_Votante v
                LEFT JOIN dbo.TB_Recinto r ON r.IdRecinto = TRY_CAST(v.IdRecintoOk AS UNIQUEIDENTIFIER)
                LEFT JOIN dbo.Territorio tMun ON tMun.IdTerritorio = r.IdMunicipio
                WHERE (@IdTerritorio IS NULL OR r.IdMunicipio = @IdTerritorio OR tMun.IdTerritorioPadre = @IdTerritorio)
                  AND (v.EstadoDiaD = 'YA_VOTO' OR v.FechaMarcaDiaD IS NOT NULL OR v.IdUsuarioMarcaDiaD IS NOT NULL OR v.ObservacionDiaD IS NOT NULL);
                SET @TotalAfectadas = @TotalAfectadas + @@ROWCOUNT;

                -- PersonaMovilizada
                UPDATE pm
                SET pm.EstadoDiaD = 'PENDIENTE',
                    pm.FechaMarcaDiaD = NULL,
                    pm.ObservacionDiaD = NULL
                FROM dbo.PersonaMovilizada pm
                LEFT JOIN dbo.Usuario uMov ON uMov.IdUsuario = pm.IdUsuarioMovilizador
                LEFT JOIN dbo.Territorio tPM ON tPM.IdTerritorio = pm.IdTerritorio
                LEFT JOIN dbo.Territorio tUser ON tUser.IdTerritorio = uMov.IdTerritorio
                WHERE (@IdTerritorio IS NULL 
                       OR pm.IdTerritorio = @IdTerritorio 
                       OR uMov.IdTerritorio = @IdTerritorio 
                       OR tPM.IdTerritorioPadre = @IdTerritorio 
                       OR tUser.IdTerritorioPadre = @IdTerritorio)
                  AND (pm.EstadoDiaD <> 'PENDIENTE' OR pm.FechaMarcaDiaD IS NOT NULL OR pm.ObservacionDiaD IS NOT NULL);
                SET @TotalAfectadas = @TotalAfectadas + @@ROWCOUNT;

                -- Limpiar en PersonaMovilizada por matching de CI con votantes del territorio
                IF (@IdTerritorio IS NOT NULL)
                BEGIN
                    UPDATE pm
                    SET pm.EstadoDiaD = 'PENDIENTE',
                        pm.FechaMarcaDiaD = NULL,
                        pm.ObservacionDiaD = NULL
                    FROM dbo.PersonaMovilizada pm
                    INNER JOIN dbo.TB_Votante v ON LTRIM(RTRIM(v.CI)) = LTRIM(RTRIM(pm.CI))
                    LEFT JOIN dbo.TB_Recinto r ON r.IdRecinto = TRY_CAST(v.IdRecintoOk AS UNIQUEIDENTIFIER)
                    LEFT JOIN dbo.Territorio tMun ON tMun.IdTerritorio = r.IdMunicipio
                    WHERE (r.IdMunicipio = @IdTerritorio OR tMun.IdTerritorioPadre = @IdTerritorio)
                      AND (pm.EstadoDiaD <> 'PENDIENTE' OR pm.FechaMarcaDiaD IS NOT NULL OR pm.ObservacionDiaD IS NOT NULL);
                    SET @TotalAfectadas = @TotalAfectadas + @@ROWCOUNT;
                END

                -- Limpiar columnas opcionales de PersonaMovilizada si existen
                IF COL_LENGTH('dbo.PersonaMovilizada', 'YaVoto') IS NOT NULL
                BEGIN
                    DECLARE @sqlYaVoto NVARCHAR(MAX) = '
                        UPDATE pm SET pm.YaVoto = 0
                        FROM dbo.PersonaMovilizada pm
                        LEFT JOIN dbo.Usuario uMov ON uMov.IdUsuario = pm.IdUsuarioMovilizador
                        LEFT JOIN dbo.Territorio tPM ON tPM.IdTerritorio = pm.IdTerritorio
                        WHERE (@IdTerritorio IS NULL OR pm.IdTerritorio = @IdTerritorio OR uMov.IdTerritorio = @IdTerritorio) AND ISNULL(pm.YaVoto, 0) = 1;';
                    EXEC sp_executesql @sqlYaVoto, N'@IdTerritorio INT', @IdTerritorio;
                END

                IF COL_LENGTH('dbo.PersonaMovilizada', 'FechaVoto') IS NOT NULL
                BEGIN
                    DECLARE @sqlFechaVoto NVARCHAR(MAX) = '
                        UPDATE pm SET pm.FechaVoto = NULL
                        FROM dbo.PersonaMovilizada pm
                        LEFT JOIN dbo.Usuario uMov ON uMov.IdUsuario = pm.IdUsuarioMovilizador
                        LEFT JOIN dbo.Territorio tPM ON tPM.IdTerritorio = pm.IdTerritorio
                        WHERE (@IdTerritorio IS NULL OR pm.IdTerritorio = @IdTerritorio OR uMov.IdTerritorio = @IdTerritorio) AND pm.FechaVoto IS NOT NULL;';
                    EXEC sp_executesql @sqlFechaVoto, N'@IdTerritorio INT', @IdTerritorio;
                END

                -- TB_DiaDHistorial (si existe la tabla)
                IF OBJECT_ID('dbo.TB_DiaDHistorial') IS NOT NULL
                BEGIN
                    DELETE h
                    FROM dbo.TB_DiaDHistorial h
                    LEFT JOIN dbo.PersonaMovilizada pm ON pm.IdPersonaMovilizada = h.IdPersonaMovilizada
                    LEFT JOIN dbo.Usuario uMov ON uMov.IdUsuario = pm.IdUsuarioMovilizador
                    LEFT JOIN dbo.Territorio tPM ON tPM.IdTerritorio = pm.IdTerritorio
                    LEFT JOIN dbo.Territorio tUser ON tUser.IdTerritorio = uMov.IdTerritorio
                    WHERE (@IdTerritorio IS NULL 
                           OR pm.IdTerritorio = @IdTerritorio 
                           OR uMov.IdTerritorio = @IdTerritorio 
                           OR tPM.IdTerritorioPadre = @IdTerritorio 
                           OR tUser.IdTerritorioPadre = @IdTerritorio);
                END
            END

            -- 2. LIMPIAR GASOLINA / COMBUSTIBLE (TB_Votante y PersonaMovilizada)
            IF (@LimpiarGasolina = 1)
            BEGIN
                -- TB_Votante
                UPDATE v
                SET v.Combustible = 0,
                    v.FechaCombustible = NULL,
                    v.IdUsuarioMarcaCombustible = NULL
                FROM dbo.TB_Votante v
                LEFT JOIN dbo.TB_Recinto r ON r.IdRecinto = TRY_CAST(v.IdRecintoOk AS UNIQUEIDENTIFIER)
                LEFT JOIN dbo.Territorio tMun ON tMun.IdTerritorio = r.IdMunicipio
                WHERE (@IdTerritorio IS NULL OR r.IdMunicipio = @IdTerritorio OR tMun.IdTerritorioPadre = @IdTerritorio)
                  AND (ISNULL(v.Combustible, 0) = 1 OR v.FechaCombustible IS NOT NULL OR v.IdUsuarioMarcaCombustible IS NOT NULL);
                SET @TotalAfectadas = @TotalAfectadas + @@ROWCOUNT;

                -- PersonaMovilizada (si existen columnas de combustible)
                IF COL_LENGTH('dbo.PersonaMovilizada', 'RecibioCombustible') IS NOT NULL
                BEGIN
                    DECLARE @sqlRecComb NVARCHAR(MAX) = '
                        UPDATE pm SET pm.RecibioCombustible = 0
                        FROM dbo.PersonaMovilizada pm
                        LEFT JOIN dbo.Usuario uMov ON uMov.IdUsuario = pm.IdUsuarioMovilizador
                        WHERE (@IdTerritorio IS NULL OR uMov.IdTerritorio = @IdTerritorio) AND ISNULL(pm.RecibioCombustible, 0) = 1;';
                    EXEC sp_executesql @sqlRecComb, N'@IdTerritorio INT', @IdTerritorio;
                END

                IF COL_LENGTH('dbo.PersonaMovilizada', 'Combustible') IS NOT NULL
                BEGIN
                    DECLARE @sqlCombPM NVARCHAR(MAX) = '
                        UPDATE pm SET pm.Combustible = 0, pm.FechaCombustible = NULL, pm.IdUsuarioMarcaCombustible = NULL
                        FROM dbo.PersonaMovilizada pm
                        LEFT JOIN dbo.Usuario uMov ON uMov.IdUsuario = pm.IdUsuarioMovilizador
                        WHERE (@IdTerritorio IS NULL OR uMov.IdTerritorio = @IdTerritorio) AND (ISNULL(pm.Combustible, 0) = 1 OR pm.FechaCombustible IS NOT NULL);';
                    EXEC sp_executesql @sqlCombPM, N'@IdTerritorio INT', @IdTerritorio;
                END
            END

            -- 3. LIMPIAR PASÓ POR EL PC (TB_Votante y PersonaMovilizada)
            IF (@LimpiarPasoPC = 1)
            BEGIN
                -- TB_Votante
                UPDATE v
                SET v.PasoPorElPC = 0,
                    v.FechaPasoPorElPC = NULL,
                    v.IdUsuarioMarcaPasoPC = NULL,
                    v.ObservacionPasoPorElPC = NULL
                FROM dbo.TB_Votante v
                LEFT JOIN dbo.TB_Recinto r ON r.IdRecinto = TRY_CAST(v.IdRecintoOk AS UNIQUEIDENTIFIER)
                LEFT JOIN dbo.Territorio tMun ON tMun.IdTerritorio = r.IdMunicipio
                WHERE (@IdTerritorio IS NULL OR r.IdMunicipio = @IdTerritorio OR tMun.IdTerritorioPadre = @IdTerritorio)
                  AND (ISNULL(v.PasoPorElPC, 0) = 1 OR v.FechaPasoPorElPC IS NOT NULL OR v.IdUsuarioMarcaPasoPC IS NOT NULL OR v.ObservacionPasoPorElPC IS NOT NULL);
                SET @TotalAfectadas = @TotalAfectadas + @@ROWCOUNT;

                -- PersonaMovilizada (si existen columnas de PC)
                IF COL_LENGTH('dbo.PersonaMovilizada', 'PasoPorElPC') IS NOT NULL
                BEGIN
                    DECLARE @sqlPCPersona NVARCHAR(MAX) = '
                        UPDATE pm 
                        SET pm.PasoPorElPC = 0' +
                        CASE WHEN COL_LENGTH('dbo.PersonaMovilizada', 'FechaPasoPorElPC') IS NOT NULL THEN ', pm.FechaPasoPorElPC = NULL' ELSE '' END +
                        CASE WHEN COL_LENGTH('dbo.PersonaMovilizada', 'IdUsuarioMarcaPasoPC') IS NOT NULL THEN ', pm.IdUsuarioMarcaPasoPC = NULL' ELSE '' END +
                        CASE WHEN COL_LENGTH('dbo.PersonaMovilizada', 'ObservacionPasoPorElPC') IS NOT NULL THEN ', pm.ObservacionPasoPorElPC = NULL' ELSE '' END +
                        ' FROM dbo.PersonaMovilizada pm
                          LEFT JOIN dbo.Usuario uMov ON uMov.IdUsuario = pm.IdUsuarioMovilizador
                          WHERE (@IdTerritorio IS NULL OR uMov.IdTerritorio = @IdTerritorio) AND (ISNULL(pm.PasoPorElPC, 0) = 1);';
                    EXEC sp_executesql @sqlPCPersona, N'@IdTerritorio INT', @IdTerritorio;
                END
            END

            SELECT @TotalAfectadas AS TotalAfectadas;";

        var dt = EjecutarSQL(
            sql,
            new SqlParameter("@LimpiarVoto", SqlDbType.Bit) { Value = limpiarVoto },
            new SqlParameter("@LimpiarGasolina", SqlDbType.Bit) { Value = limpiarGasolina },
            new SqlParameter("@LimpiarPasoPC", SqlDbType.Bit) { Value = limpiarPasoPC },
            new SqlParameter("@IdTerritorio", SqlDbType.Int) { Value = (object?)idTerritorio ?? DBNull.Value }
        );

        if (dt != null && dt.Rows.Count > 0 && dt.Rows[0]["TotalAfectadas"] != DBNull.Value)
        {
            return Convert.ToInt32(dt.Rows[0]["TotalAfectadas"]);
        }
        return 0;
    }
}