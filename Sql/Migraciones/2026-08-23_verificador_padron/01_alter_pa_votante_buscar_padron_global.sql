-- =========================================================================
-- Procedimiento: PA_VOTANTE_BUSCAR_PADRON_GLOBAL
-- Descripción: Búsqueda indexada en el padrón electoral con coincidencia
--              exacta por IdRecinto (TRIM) y Mesa (TRIM), además de
--              búsqueda flexible por CI, nombres y apellidos.
-- =========================================================================

CREATE OR ALTER PROCEDURE dbo.PA_VOTANTE_BUSCAR_PADRON_GLOBAL
    @Texto VARCHAR(100) = '',
    @IdRecinto VARCHAR(150) = NULL,
    @NroMesa VARCHAR(50) = NULL,
    @IdTerritorio INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SET @Texto = LTRIM(RTRIM(ISNULL(@Texto, '')));
    SET @IdRecinto = NULLIF(LTRIM(RTRIM(ISNULL(@IdRecinto, ''))), '');
    SET @NroMesa = NULLIF(LTRIM(RTRIM(ISNULL(@NroMesa, ''))), '');

    DECLARE @EsNumero BIT = 0;
    IF (@Texto <> '' AND @Texto NOT LIKE '%[^0-9]%')
        SET @EsNumero = 1;

    DECLARE @IdRecintoGuid VARCHAR(50) = NULL;
    DECLARE @IdRecintoLegado VARCHAR(50) = NULL;

    IF (@IdRecinto IS NOT NULL)
    BEGIN
        IF (TRY_CAST(@IdRecinto AS UNIQUEIDENTIFIER) IS NOT NULL)
            SET @IdRecintoGuid = @IdRecinto;
        ELSE
            SET @IdRecintoLegado = @IdRecinto;
    END;

    CREATE TABLE #Resultados (
        IdVotante VARCHAR(150) COLLATE Modern_Spanish_CI_AS,
        Nombres VARCHAR(150) COLLATE Modern_Spanish_CI_AS,
        Apellidos VARCHAR(150) COLLATE Modern_Spanish_CI_AS,
        NombreCompleto VARCHAR(300) COLLATE Modern_Spanish_CI_AS,
        CI VARCHAR(30) COLLATE Modern_Spanish_CI_AS PRIMARY KEY,
        EstadoRegistro VARCHAR(50) COLLATE Modern_Spanish_CI_AS,
        EstadoDiaD VARCHAR(50) COLLATE Modern_Spanish_CI_AS,
        FechaRegistro DATETIME,
        FechaMarcaDiaD DATETIME,
        IdUsuarioMarcaDiaD INT,
        Sexo VARCHAR(20) COLLATE Modern_Spanish_CI_AS,
        IdRecintoLegado VARCHAR(150) COLLATE Modern_Spanish_CI_AS,
        RecintoVotacion VARCHAR(250) COLLATE Modern_Spanish_CI_AS,
        Distrito VARCHAR(150) COLLATE Modern_Spanish_CI_AS,
        Departamento VARCHAR(150) COLLATE Modern_Spanish_CI_AS,
        NroMesa VARCHAR(50) COLLATE Modern_Spanish_CI_AS,
        NroOrden INT,
        IdRecinto VARCHAR(50) COLLATE Modern_Spanish_CI_AS,
        PasoPorElPC BIT,
        FechaPasoPorElPC DATETIME,
        IdUsuarioMarcaPasoPC INT,
        PerteneceAOtroRecinto BIT
    );

    -- ============================================================
    -- CASO A: Filtro por Recinto GUID
    -- ============================================================
    IF (@IdRecintoGuid IS NOT NULL)
    BEGIN
        -- 1. Carga inicial del recinto (Texto vacío)
        IF (@Texto = '')
        BEGIN
            IF (@NroMesa IS NOT NULL)
            BEGIN
                INSERT INTO #Resultados
                SELECT TOP 300
                    v.IdVotante, v.Nombres, v.Apellidos,
                    LTRIM(RTRIM(ISNULL(v.Nombres, ''))) + ' ' + LTRIM(RTRIM(ISNULL(v.Apellidos, ''))),
                    v.CI, v.EstadoRegistro, ISNULL(v.EstadoDiaD, 'PENDIENTE'),
                    NULL, NULL, 0, v.Sexo,
                    NULL, v.RecintoVotacion, NULL, NULL, v.NroMesa, v.OrdenMesa, v.IdRecintoOk,
                    ISNULL(v.PasoPorElPC, 0), v.FechaPasoPorElPC, NULL,
                    0
                FROM dbo.TB_Votante v WITH (INDEX(IX_TB_Votante_IdRecintoOk), NOLOCK)
                WHERE v.IdRecintoOk = @IdRecintoGuid AND v.NroMesa = @NroMesa
                ORDER BY v.NroMesa ASC, v.OrdenMesa ASC
                OPTION (RECOMPILE);
            END
            ELSE
            BEGIN
                INSERT INTO #Resultados
                SELECT TOP 300
                    v.IdVotante, v.Nombres, v.Apellidos,
                    LTRIM(RTRIM(ISNULL(v.Nombres, ''))) + ' ' + LTRIM(RTRIM(ISNULL(v.Apellidos, ''))),
                    v.CI, v.EstadoRegistro, ISNULL(v.EstadoDiaD, 'PENDIENTE'),
                    NULL, NULL, 0, v.Sexo,
                    NULL, v.RecintoVotacion, NULL, NULL, v.NroMesa, v.OrdenMesa, v.IdRecintoOk,
                    ISNULL(v.PasoPorElPC, 0), v.FechaPasoPorElPC, NULL,
                    0
                FROM dbo.TB_Votante v WITH (INDEX(IX_TB_Votante_IdRecintoOk), NOLOCK)
                WHERE v.IdRecintoOk = @IdRecintoGuid
                ORDER BY v.NroMesa ASC, v.OrdenMesa ASC
                OPTION (RECOMPILE);
            END;
        END
        -- 2. Búsqueda numérica (CI exacto/prefijo o Nro de Orden) en propio recinto
        ELSE IF (@EsNumero = 1)
        BEGIN
            -- Búsqueda por CI exacto con índice de CI
            INSERT INTO #Resultados
            SELECT TOP 50
                v.IdVotante, v.Nombres, v.Apellidos,
                LTRIM(RTRIM(ISNULL(v.Nombres, ''))) + ' ' + LTRIM(RTRIM(ISNULL(v.Apellidos, ''))),
                v.CI, v.EstadoRegistro, ISNULL(v.EstadoDiaD, 'PENDIENTE'),
                NULL, NULL, 0, v.Sexo,
                NULL, v.RecintoVotacion, v.Distrito, v.Departamento, v.NroMesa, v.OrdenMesa, v.IdRecintoOk,
                ISNULL(v.PasoPorElPC, 0), v.FechaPasoPorElPC, NULL,
                0
            FROM dbo.TB_Votante v WITH (INDEX(IX_TB_Votante_CI), NOLOCK)
            WHERE v.CI = @Texto
              AND v.IdRecintoOk = @IdRecintoGuid
              AND (@NroMesa IS NULL OR v.NroMesa = @NroMesa)
            OPTION (RECOMPILE);

            -- Si no coincide exacto por CI, buscar por Orden o CI prefijo dentro del recinto
            IF NOT EXISTS (SELECT 1 FROM #Resultados)
            BEGIN
                INSERT INTO #Resultados
                SELECT TOP 50
                    v.IdVotante, v.Nombres, v.Apellidos,
                    LTRIM(RTRIM(ISNULL(v.Nombres, ''))) + ' ' + LTRIM(RTRIM(ISNULL(v.Apellidos, ''))),
                    v.CI, v.EstadoRegistro, ISNULL(v.EstadoDiaD, 'PENDIENTE'),
                    NULL, NULL, 0, v.Sexo,
                    NULL, v.RecintoVotacion, NULL, NULL, v.NroMesa, v.OrdenMesa, v.IdRecintoOk,
                    ISNULL(v.PasoPorElPC, 0), v.FechaPasoPorElPC, NULL,
                    0
                FROM dbo.TB_Votante v WITH (INDEX(IX_TB_Votante_IdRecintoOk), NOLOCK)
                WHERE v.IdRecintoOk = @IdRecintoGuid
                  AND (@NroMesa IS NULL OR v.NroMesa = @NroMesa)
                  AND (v.CI LIKE @Texto + '%' OR v.OrdenMesa = TRY_CAST(@Texto AS INT))
                ORDER BY v.NroMesa ASC, v.OrdenMesa ASC
                OPTION (RECOMPILE);
            END;
        END
        -- 3. Búsqueda por Nombre dentro del propio recinto
        ELSE
        BEGIN
            INSERT INTO #Resultados
            SELECT TOP 50
                v.IdVotante, v.Nombres, v.Apellidos,
                LTRIM(RTRIM(ISNULL(v.Nombres, ''))) + ' ' + LTRIM(RTRIM(ISNULL(v.Apellidos, ''))),
                v.CI, v.EstadoRegistro, ISNULL(v.EstadoDiaD, 'PENDIENTE'),
                NULL, NULL, 0, v.Sexo,
                NULL, v.RecintoVotacion, NULL, NULL, v.NroMesa, v.OrdenMesa, v.IdRecintoOk,
                ISNULL(v.PasoPorElPC, 0), v.FechaPasoPorElPC, NULL,
                0
            FROM dbo.TB_Votante v WITH (INDEX(IX_TB_Votante_IdRecintoOk), NOLOCK)
            WHERE v.IdRecintoOk = @IdRecintoGuid
              AND (@NroMesa IS NULL OR v.NroMesa = @NroMesa)
              AND (v.Nombres LIKE '%' + @Texto + '%' OR v.Apellidos LIKE '%' + @Texto + '%')
            ORDER BY v.NroMesa ASC, v.OrdenMesa ASC
            OPTION (RECOMPILE);
        END;

        -- 4. SOLO SI NO SE ENCONTRÓ NADA EN MI RECINTO y es búsqueda numérica (CI):
        -- Búsqueda estrictamente EXACTA por CI en otros recintos (sin LIKE)
        IF NOT EXISTS (SELECT 1 FROM #Resultados) AND @Texto <> '' AND @EsNumero = 1
        BEGIN
            INSERT INTO #Resultados
            SELECT TOP 5
                v.IdVotante, v.Nombres, v.Apellidos,
                LTRIM(RTRIM(ISNULL(v.Nombres, ''))) + ' ' + LTRIM(RTRIM(ISNULL(v.Apellidos, ''))),
                v.CI, v.EstadoRegistro, ISNULL(v.EstadoDiaD, 'PENDIENTE'),
                NULL, NULL, 0, v.Sexo,
                v.IdRecinto, v.RecintoVotacion, v.Distrito, v.Departamento, v.NroMesa, v.OrdenMesa, v.IdRecintoOk,
                ISNULL(v.PasoPorElPC, 0), v.FechaPasoPorElPC, NULL,
                1
            FROM dbo.TB_Votante v WITH (INDEX(IX_TB_Votante_CI), NOLOCK)
            WHERE v.CI = @Texto
            OPTION (RECOMPILE);
        END;
    END
    -- ============================================================
    -- CASO B: Filtro por Recinto Legado (si no era GUID)
    -- ============================================================
    ELSE IF (@IdRecintoLegado IS NOT NULL)
    BEGIN
        IF (@Texto = '')
        BEGIN
            INSERT INTO #Resultados
            SELECT TOP 300
                v.IdVotante, v.Nombres, v.Apellidos,
                LTRIM(RTRIM(ISNULL(v.Nombres, ''))) + ' ' + LTRIM(RTRIM(ISNULL(v.Apellidos, ''))),
                v.CI, v.EstadoRegistro, ISNULL(v.EstadoDiaD, 'PENDIENTE'),
                NULL, NULL, 0, v.Sexo,
                v.IdRecinto, v.RecintoVotacion, NULL, NULL, v.NroMesa, v.OrdenMesa, v.IdRecintoOk,
                ISNULL(v.PasoPorElPC, 0), v.FechaPasoPorElPC, NULL,
                0
            FROM dbo.TB_Votante v WITH (INDEX(IX_TB_Votante_IdRecinto), NOLOCK)
            WHERE v.IdRecinto = @IdRecintoLegado
              AND (@NroMesa IS NULL OR v.NroMesa = @NroMesa)
            ORDER BY v.NroMesa ASC, v.OrdenMesa ASC
            OPTION (RECOMPILE);
        END
        ELSE IF (@EsNumero = 1)
        BEGIN
            INSERT INTO #Resultados
            SELECT TOP 50
                v.IdVotante, v.Nombres, v.Apellidos,
                LTRIM(RTRIM(ISNULL(v.Nombres, ''))) + ' ' + LTRIM(RTRIM(ISNULL(v.Apellidos, ''))),
                v.CI, v.EstadoRegistro, ISNULL(v.EstadoDiaD, 'PENDIENTE'),
                NULL, NULL, 0, v.Sexo,
                v.IdRecinto, v.RecintoVotacion, v.Distrito, v.Departamento, v.NroMesa, v.OrdenMesa, v.IdRecintoOk,
                ISNULL(v.PasoPorElPC, 0), v.FechaPasoPorElPC, NULL,
                0
            FROM dbo.TB_Votante v WITH (INDEX(IX_TB_Votante_CI), NOLOCK)
            WHERE v.CI = @Texto
              AND v.IdRecinto = @IdRecintoLegado
              AND (@NroMesa IS NULL OR v.NroMesa = @NroMesa)
            OPTION (RECOMPILE);

            IF NOT EXISTS (SELECT 1 FROM #Resultados)
            BEGIN
                INSERT INTO #Resultados
                SELECT TOP 50
                    v.IdVotante, v.Nombres, v.Apellidos,
                    LTRIM(RTRIM(ISNULL(v.Nombres, ''))) + ' ' + LTRIM(RTRIM(ISNULL(v.Apellidos, ''))),
                    v.CI, v.EstadoRegistro, ISNULL(v.EstadoDiaD, 'PENDIENTE'),
                    NULL, NULL, 0, v.Sexo,
                    v.IdRecinto, v.RecintoVotacion, NULL, NULL, v.NroMesa, v.OrdenMesa, v.IdRecintoOk,
                    ISNULL(v.PasoPorElPC, 0), v.FechaPasoPorElPC, NULL,
                    0
                FROM dbo.TB_Votante v WITH (INDEX(IX_TB_Votante_IdRecinto), NOLOCK)
                WHERE v.IdRecinto = @IdRecintoLegado
                  AND (@NroMesa IS NULL OR v.NroMesa = @NroMesa)
                  AND (v.CI LIKE @Texto + '%' OR v.OrdenMesa = TRY_CAST(@Texto AS INT))
                ORDER BY v.NroMesa ASC, v.OrdenMesa ASC
                OPTION (RECOMPILE);
            END;
        END
        ELSE
        BEGIN
            INSERT INTO #Resultados
            SELECT TOP 50
                v.IdVotante, v.Nombres, v.Apellidos,
                LTRIM(RTRIM(ISNULL(v.Nombres, ''))) + ' ' + LTRIM(RTRIM(ISNULL(v.Apellidos, ''))),
                v.CI, v.EstadoRegistro, ISNULL(v.EstadoDiaD, 'PENDIENTE'),
                NULL, NULL, 0, v.Sexo,
                v.IdRecinto, v.RecintoVotacion, NULL, NULL, v.NroMesa, v.OrdenMesa, v.IdRecintoOk,
                ISNULL(v.PasoPorElPC, 0), v.FechaPasoPorElPC, NULL,
                0
            FROM dbo.TB_Votante v WITH (INDEX(IX_TB_Votante_IdRecinto), NOLOCK)
            WHERE v.IdRecinto = @IdRecintoLegado
              AND (@NroMesa IS NULL OR v.NroMesa = @NroMesa)
              AND (v.Nombres LIKE '%' + @Texto + '%' OR v.Apellidos LIKE '%' + @Texto + '%')
            ORDER BY v.NroMesa ASC, v.OrdenMesa ASC
            OPTION (RECOMPILE);
        END;

        -- Fallback exacto en otros recintos
        IF NOT EXISTS (SELECT 1 FROM #Resultados) AND @Texto <> '' AND @EsNumero = 1
        BEGIN
            INSERT INTO #Resultados
            SELECT TOP 5
                v.IdVotante, v.Nombres, v.Apellidos,
                LTRIM(RTRIM(ISNULL(v.Nombres, ''))) + ' ' + LTRIM(RTRIM(ISNULL(v.Apellidos, ''))),
                v.CI, v.EstadoRegistro, ISNULL(v.EstadoDiaD, 'PENDIENTE'),
                NULL, NULL, 0, v.Sexo,
                v.IdRecinto, v.RecintoVotacion, v.Distrito, v.Departamento, v.NroMesa, v.OrdenMesa, v.IdRecintoOk,
                ISNULL(v.PasoPorElPC, 0), v.FechaPasoPorElPC, NULL,
                1
            FROM dbo.TB_Votante v WITH (INDEX(IX_TB_Votante_CI), NOLOCK)
            WHERE v.CI = @Texto
            OPTION (RECOMPILE);
        END;
    END
    -- ============================================================
    -- CASO C: Búsqueda Global / Por Territorio (Sin recinto)
    -- ============================================================
    ELSE
    BEGIN
        IF (@Texto = '')
        BEGIN
            INSERT INTO #Resultados
            SELECT TOP 100
                v.IdVotante, v.Nombres, v.Apellidos,
                LTRIM(RTRIM(ISNULL(v.Nombres, ''))) + ' ' + LTRIM(RTRIM(ISNULL(v.Apellidos, ''))),
                v.CI, v.EstadoRegistro, ISNULL(v.EstadoDiaD, 'PENDIENTE'),
                NULL, NULL, 0, v.Sexo,
                v.IdRecinto, v.RecintoVotacion, v.Distrito, v.Departamento, v.NroMesa, v.OrdenMesa, v.IdRecintoOk,
                ISNULL(v.PasoPorElPC, 0), v.FechaPasoPorElPC, NULL,
                0
            FROM dbo.TB_Votante v WITH (NOLOCK)
            WHERE (
                @IdTerritorio IS NULL 
                OR v.IdRecintoOk IN (SELECT CAST(r.IdRecinto AS VARCHAR(50)) FROM dbo.TB_Recinto r WITH (NOLOCK) WHERE r.IdMunicipio = @IdTerritorio)
            )
            ORDER BY v.CI ASC
            OPTION (RECOMPILE);
        END
        ELSE IF (@EsNumero = 1)
        BEGIN
            INSERT INTO #Resultados
            SELECT TOP 50
                v.IdVotante, v.Nombres, v.Apellidos,
                LTRIM(RTRIM(ISNULL(v.Nombres, ''))) + ' ' + LTRIM(RTRIM(ISNULL(v.Apellidos, ''))),
                v.CI, v.EstadoRegistro, ISNULL(v.EstadoDiaD, 'PENDIENTE'),
                NULL, NULL, 0, v.Sexo,
                v.IdRecinto, v.RecintoVotacion, v.Distrito, v.Departamento, v.NroMesa, v.OrdenMesa, v.IdRecintoOk,
                ISNULL(v.PasoPorElPC, 0), v.FechaPasoPorElPC, NULL,
                0
            FROM dbo.TB_Votante v WITH (INDEX(IX_TB_Votante_CI), NOLOCK)
            WHERE v.CI = @Texto
            OPTION (RECOMPILE);
        END
        ELSE
        BEGIN
            INSERT INTO #Resultados
            SELECT TOP 50
                v.IdVotante, v.Nombres, v.Apellidos,
                LTRIM(RTRIM(ISNULL(v.Nombres, ''))) + ' ' + LTRIM(RTRIM(ISNULL(v.Apellidos, ''))),
                v.CI, v.EstadoRegistro, ISNULL(v.EstadoDiaD, 'PENDIENTE'),
                NULL, NULL, 0, v.Sexo,
                v.IdRecinto, v.RecintoVotacion, v.Distrito, v.Departamento, v.NroMesa, v.OrdenMesa, v.IdRecintoOk,
                ISNULL(v.PasoPorElPC, 0), v.FechaPasoPorElPC, NULL,
                0
            FROM dbo.TB_Votante v WITH (NOLOCK)
            WHERE (v.Nombres LIKE '%' + @Texto + '%' OR v.Apellidos LIKE '%' + @Texto + '%')
            OPTION (RECOMPILE);
        END;
    END;

    -- ============================================================
    -- SALIDA FINAL ENRIQUECIDA CON CRUCE 1X10 Y USUARIOS
    -- ============================================================
    SELECT 
        u.IdVotante,
        u.Nombres,
        u.Apellidos,
        u.NombreCompleto,
        u.CI,
        u.EstadoRegistro,
        u.EstadoDiaD,
        u.FechaRegistro,
        u.FechaMarcaDiaD,
        u.IdUsuarioMarcaDiaD,
        uMarcaVoto.NombreCompleto AS NombreUsuarioMarcaDiaD,
        uMarcaVoto.Usuario AS UsuarioMarcaDiaD,
        uSupVoto.NombreCompleto AS SupervisorUsuarioMarcaDiaD,
        u.Sexo,
        u.IdRecintoLegado,
        u.RecintoVotacion,
        COALESCE(u.Distrito, tMun.Nombre, '') AS Municipio,
        COALESCE(u.Distrito, tMun.Nombre, '') AS Distrito,
        COALESCE(u.Departamento, tDep.Nombre, '') AS Departamento,
        u.NroMesa,
        u.NroOrden,
        u.IdRecinto,
        u.PasoPorElPC,
        u.FechaPasoPorElPC,
        u.IdUsuarioMarcaPasoPC,
        uMarcaPC.NombreCompleto AS NombreUsuarioMarcaPasoPC,
        uMarcaPC.Usuario AS UsuarioMarcaPasoPC,
        uSupPC.NombreCompleto AS SupervisorUsuarioMarcaPasoPC,
        CASE WHEN pm.IdPersonaMovilizada IS NOT NULL THEN 1 ELSE 0 END AS EsEstructura1x10,
        pm.IdPersonaMovilizada,
        pm.Celular AS CelularVotante,
        uMov.NombreCompleto AS Movilizador,
        uMov.Celular AS CelularMovilizador,
        uGer.NombreCompleto AS Gerente,
        uGer.Celular AS CelularGerente,
        u.PerteneceAOtroRecinto
    FROM #Resultados u
    LEFT JOIN dbo.TB_Recinto rRec WITH (NOLOCK) ON rRec.IdRecinto = TRY_CAST(u.IdRecinto AS UNIQUEIDENTIFIER)
    LEFT JOIN dbo.Territorio tMun WITH (NOLOCK) ON tMun.IdTerritorio = rRec.IdMunicipio
    LEFT JOIN dbo.Territorio tDep WITH (NOLOCK) ON tDep.IdTerritorio = tMun.IdTerritorioPadre
    LEFT JOIN dbo.PersonaMovilizada pm WITH (NOLOCK) 
        ON pm.CI = u.CI AND (pm.Activo IS NULL OR pm.Activo = 1)
    LEFT JOIN dbo.Usuario uMov WITH (NOLOCK) ON uMov.IdUsuario = pm.IdUsuarioMovilizador
    LEFT JOIN dbo.Usuario uGer WITH (NOLOCK) ON uGer.IdUsuario = uMov.IdUsuarioSupervisor
    LEFT JOIN dbo.Usuario uMarcaVoto WITH (NOLOCK) ON uMarcaVoto.IdUsuario = u.IdUsuarioMarcaDiaD
    LEFT JOIN dbo.Usuario uSupVoto WITH (NOLOCK) ON uSupVoto.IdUsuario = uMarcaVoto.IdUsuarioSupervisor
    LEFT JOIN dbo.Usuario uMarcaPC WITH (NOLOCK) ON uMarcaPC.IdUsuario = u.IdUsuarioMarcaPasoPC
    LEFT JOIN dbo.Usuario uSupPC WITH (NOLOCK) ON uSupPC.IdUsuario = uMarcaPC.IdUsuarioSupervisor
    ORDER BY 
        u.PerteneceAOtroRecinto ASC,
        CASE WHEN pm.IdPersonaMovilizada IS NOT NULL THEN 0 ELSE 1 END ASC,
        TRY_CAST(u.NroMesa AS INT) ASC, 
        u.NroOrden ASC
    OPTION (RECOMPILE);

    DROP TABLE #Resultados;
END;
GO

-- =========================================================================
-- Procedimiento: PA_ObtenerVotante
-- =========================================================================
CREATE OR ALTER PROCEDURE dbo.PA_ObtenerVotante
    @CI VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SET @CI = LTRIM(RTRIM(ISNULL(@CI, '')));

    SELECT TOP 10
        v.IdVotante,
        v.Nombres,
        v.Apellidos,
        v.CI,
        v.EstadoRegistro,
        v.EstadoDiaD,
        v.FechaRegistro,
        v.FechaMarcaDiaD,
        v.Sexo,
        v.IdRecinto,
        v.RecintoVotacion,
        ISNULL(v.NroMesa, v.Mesa) AS NroMesa,
        ISNULL(v.NroOrden, v.Orden) AS NroOrden
    FROM dbo.Votante v WITH (NOLOCK)
    WHERE v.CI = @CI OR v.CI LIKE @CI + '%'
    ORDER BY v.IdVotante ASC;
END;
GO
