-- =========================================================================
-- Migración: 06_alter_pa_paso_pc_y_votantes_marcados_observacion.sql
-- Descripción:
-- 1. Agrega columna ObservacionPasoPorElPC a TB_Votante si no existe.
-- 2. Actualiza PA_VOTANTE_MARCAR_PASO_PC para persistir la observación.
-- 3. Actualiza pa_veedor_votantes_marcados_listar para retornar ObservacionPasoPorElPC.
-- 4. Actualiza PA_VOTANTE_BUSCAR_PADRON_GLOBAL para retornar ObservacionPasoPorElPC.
-- =========================================================================

-- 1. Agregar columna a TB_Votante
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('TB_Votante') AND name = 'ObservacionPasoPorElPC')
BEGIN
    ALTER TABLE dbo.TB_Votante ADD ObservacionPasoPorElPC VARCHAR(500) NULL;
END
GO

-- 2. PA_VOTANTE_MARCAR_PASO_PC
CREATE OR ALTER PROCEDURE dbo.PA_VOTANTE_MARCAR_PASO_PC
    @IdVotante VARCHAR(150),
    @IdUsuarioMarca INT,
    @Observacion VARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IdGuid UNIQUEIDENTIFIER = TRY_CAST(LTRIM(RTRIM(@IdVotante)) AS UNIQUEIDENTIFIER);
    IF (@IdGuid IS NOT NULL)
    BEGIN
        UPDATE dbo.TB_Votante
        SET PasoPorElPC = 1,
            FechaPasoPorElPC = GETDATE(),
            IdUsuarioMarcaPasoPC = @IdUsuarioMarca,
            ObservacionPasoPorElPC = CASE 
                WHEN @Observacion IS NOT NULL AND LTRIM(RTRIM(@Observacion)) <> '' 
                THEN LTRIM(RTRIM(@Observacion)) 
                ELSE ObservacionPasoPorElPC 
            END
        WHERE IdVotante = @IdGuid;

        SELECT @@ROWCOUNT AS FilasAfectadas;
    END
    ELSE
    BEGIN
        UPDATE dbo.TB_Votante
        SET PasoPorElPC = 1,
            FechaPasoPorElPC = GETDATE(),
            IdUsuarioMarcaPasoPC = @IdUsuarioMarca,
            ObservacionPasoPorElPC = CASE 
                WHEN @Observacion IS NOT NULL AND LTRIM(RTRIM(@Observacion)) <> '' 
                THEN LTRIM(RTRIM(@Observacion)) 
                ELSE ObservacionPasoPorElPC 
            END
        WHERE LTRIM(RTRIM(CAST(IdVotante AS VARCHAR(150)))) = LTRIM(RTRIM(@IdVotante));

        SELECT @@ROWCOUNT AS FilasAfectadas;
    END
END;
GO

-- 3. pa_veedor_votantes_marcados_listar
CREATE OR ALTER PROCEDURE dbo.pa_veedor_votantes_marcados_listar
    @IdUsuarioMarca INT = NULL,
    @TipoMarca VARCHAR(20) = NULL,
    @IdTerritorio INT = NULL,
    @IdAdmin INT = NULL,
    @IdGerente INT = NULL,
    @IdMovilizador INT = NULL,
    @Texto VARCHAR(100) = NULL,
    @Offset INT = 0,
    @Limit INT = 1000
AS
BEGIN
    SET NOCOUNT ON;

    SET @Texto = LTRIM(RTRIM(ISNULL(@Texto, '')));
    SET @TipoMarca = UPPER(LTRIM(RTRIM(ISNULL(@TipoMarca, 'TODOS'))));

    SELECT TOP (@Limit)
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
    LEFT JOIN dbo.Usuario uSupVoto WITH (NOLOCK) ON uSupVoto.IdUsuario = uMarcaVoto.IdUsuarioSupervisor
    LEFT JOIN dbo.Usuario uMarcaPC WITH (NOLOCK) ON uMarcaPC.IdUsuario = v.IdUsuarioMarcaPasoPC
    LEFT JOIN dbo.Usuario uSupPC WITH (NOLOCK) ON uSupPC.IdUsuario = uMarcaPC.IdUsuarioSupervisor
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
        OR rRec.IdMunicipio = @IdTerritorio
        OR tMun.IdTerritorioPadre = @IdTerritorio
    )
    AND (
        @IdAdmin IS NULL
        OR uSupVoto.IdUsuarioSupervisor = @IdAdmin
        OR uSupPC.IdUsuarioSupervisor = @IdAdmin
        OR uMarcaVoto.IdUsuarioSupervisor = @IdAdmin
        OR uMarcaPC.IdUsuarioSupervisor = @IdAdmin
    )
    AND (
        @IdGerente IS NULL
        OR uGer.IdUsuario = @IdGerente
        OR uSupVoto.IdUsuario = @IdGerente
        OR uSupPC.IdUsuario = @IdGerente
        OR uMarcaVoto.IdUsuarioSupervisor = @IdGerente
        OR uMarcaPC.IdUsuarioSupervisor = @IdGerente
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
    OPTION (RECOMPILE);
END;
GO

-- 4. PA_VOTANTE_BUSCAR_PADRON_GLOBAL con ObservacionPasoPorElPC
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
        ObservacionPasoPorElPC VARCHAR(500) COLLATE Modern_Spanish_CI_AS,
        PerteneceAOtroRecinto BIT
    );

    IF (@IdRecintoGuid IS NOT NULL)
    BEGIN
        IF (@Texto = '')
        BEGIN
            INSERT INTO #Resultados
            SELECT TOP 300
                v.IdVotante, v.Nombres, v.Apellidos,
                LTRIM(RTRIM(ISNULL(v.Nombres, ''))) + ' ' + LTRIM(RTRIM(ISNULL(v.Apellidos, ''))),
                v.CI, v.EstadoRegistro, ISNULL(v.EstadoDiaD, 'PENDIENTE'),
                v.FechaRegistro, v.FechaMarcaDiaD, ISNULL(v.IdUsuarioMarcaDiaD, 0), v.Sexo,
                v.IdRecinto, v.RecintoVotacion, v.Distrito, v.Departamento, v.NroMesa, v.OrdenMesa, v.IdRecintoOk,
                ISNULL(v.PasoPorElPC, 0), v.FechaPasoPorElPC, v.IdUsuarioMarcaPasoPC,
                v.ObservacionPasoPorElPC,
                0
            FROM dbo.TB_Votante v WITH (INDEX(IX_TB_Votante_IdRecintoOk), NOLOCK)
            WHERE v.IdRecintoOk = @IdRecintoGuid
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
                v.FechaRegistro, v.FechaMarcaDiaD, ISNULL(v.IdUsuarioMarcaDiaD, 0), v.Sexo,
                v.IdRecinto, v.RecintoVotacion, v.Distrito, v.Departamento, v.NroMesa, v.OrdenMesa, v.IdRecintoOk,
                ISNULL(v.PasoPorElPC, 0), v.FechaPasoPorElPC, v.IdUsuarioMarcaPasoPC,
                v.ObservacionPasoPorElPC,
                0
            FROM dbo.TB_Votante v WITH (INDEX(IX_TB_Votante_CI), NOLOCK)
            WHERE v.CI = @Texto
              AND v.IdRecintoOk = @IdRecintoGuid
              AND (@NroMesa IS NULL OR v.NroMesa = @NroMesa)
            OPTION (RECOMPILE);

            IF NOT EXISTS (SELECT 1 FROM #Resultados)
            BEGIN
                INSERT INTO #Resultados
                SELECT TOP 50
                    v.IdVotante, v.Nombres, v.Apellidos,
                    LTRIM(RTRIM(ISNULL(v.Nombres, ''))) + ' ' + LTRIM(RTRIM(ISNULL(v.Apellidos, ''))),
                    v.CI, v.EstadoRegistro, ISNULL(v.EstadoDiaD, 'PENDIENTE'),
                    v.FechaRegistro, v.FechaMarcaDiaD, ISNULL(v.IdUsuarioMarcaDiaD, 0), v.Sexo,
                    v.IdRecinto, v.RecintoVotacion, v.Distrito, v.Departamento, v.NroMesa, v.OrdenMesa, v.IdRecintoOk,
                    ISNULL(v.PasoPorElPC, 0), v.FechaPasoPorElPC, v.IdUsuarioMarcaPasoPC,
                    v.ObservacionPasoPorElPC,
                    0
                FROM dbo.TB_Votante v WITH (INDEX(IX_TB_Votante_IdRecintoOk), NOLOCK)
                WHERE v.IdRecintoOk = @IdRecintoGuid
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
                v.FechaRegistro, v.FechaMarcaDiaD, ISNULL(v.IdUsuarioMarcaDiaD, 0), v.Sexo,
                v.IdRecinto, v.RecintoVotacion, v.Distrito, v.Departamento, v.NroMesa, v.OrdenMesa, v.IdRecintoOk,
                ISNULL(v.PasoPorElPC, 0), v.FechaPasoPorElPC, v.IdUsuarioMarcaPasoPC,
                v.ObservacionPasoPorElPC,
                0
            FROM dbo.TB_Votante v WITH (INDEX(IX_TB_Votante_IdRecintoOk), NOLOCK)
            WHERE v.IdRecintoOk = @IdRecintoGuid
              AND (@NroMesa IS NULL OR v.NroMesa = @NroMesa)
              AND (v.Nombres LIKE '%' + @Texto + '%' OR v.Apellidos LIKE '%' + @Texto + '%')
            ORDER BY v.NroMesa ASC, v.OrdenMesa ASC
            OPTION (RECOMPILE);
        END;

        IF NOT EXISTS (SELECT 1 FROM #Resultados) AND @Texto <> '' AND @EsNumero = 1
        BEGIN
            INSERT INTO #Resultados
            SELECT TOP 5
                v.IdVotante, v.Nombres, v.Apellidos,
                LTRIM(RTRIM(ISNULL(v.Nombres, ''))) + ' ' + LTRIM(RTRIM(ISNULL(v.Apellidos, ''))),
                v.CI, v.EstadoRegistro, ISNULL(v.EstadoDiaD, 'PENDIENTE'),
                v.FechaRegistro, v.FechaMarcaDiaD, ISNULL(v.IdUsuarioMarcaDiaD, 0), v.Sexo,
                v.IdRecinto, v.RecintoVotacion, v.Distrito, v.Departamento, v.NroMesa, v.OrdenMesa, v.IdRecintoOk,
                ISNULL(v.PasoPorElPC, 0), v.FechaPasoPorElPC, v.IdUsuarioMarcaPasoPC,
                v.ObservacionPasoPorElPC,
                1
            FROM dbo.TB_Votante v WITH (INDEX(IX_TB_Votante_CI), NOLOCK)
            WHERE v.CI = @Texto
            OPTION (RECOMPILE);
        END;
    END
    ELSE IF (@IdRecintoLegado IS NOT NULL)
    BEGIN
        IF (@Texto = '')
        BEGIN
            INSERT INTO #Resultados
            SELECT TOP 300
                v.IdVotante, v.Nombres, v.Apellidos,
                LTRIM(RTRIM(ISNULL(v.Nombres, ''))) + ' ' + LTRIM(RTRIM(ISNULL(v.Apellidos, ''))),
                v.CI, v.EstadoRegistro, ISNULL(v.EstadoDiaD, 'PENDIENTE'),
                v.FechaRegistro, v.FechaMarcaDiaD, ISNULL(v.IdUsuarioMarcaDiaD, 0), v.Sexo,
                v.IdRecinto, v.RecintoVotacion, v.Distrito, v.Departamento, v.NroMesa, v.OrdenMesa, v.IdRecintoOk,
                ISNULL(v.PasoPorElPC, 0), v.FechaPasoPorElPC, v.IdUsuarioMarcaPasoPC,
                v.ObservacionPasoPorElPC,
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
                v.FechaRegistro, v.FechaMarcaDiaD, ISNULL(v.IdUsuarioMarcaDiaD, 0), v.Sexo,
                v.IdRecinto, v.RecintoVotacion, v.Distrito, v.Departamento, v.NroMesa, v.OrdenMesa, v.IdRecintoOk,
                ISNULL(v.PasoPorElPC, 0), v.FechaPasoPorElPC, v.IdUsuarioMarcaPasoPC,
                v.ObservacionPasoPorElPC,
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
                    v.FechaRegistro, v.FechaMarcaDiaD, ISNULL(v.IdUsuarioMarcaDiaD, 0), v.Sexo,
                    v.IdRecinto, v.RecintoVotacion, v.Distrito, v.Departamento, v.NroMesa, v.OrdenMesa, v.IdRecintoOk,
                    ISNULL(v.PasoPorElPC, 0), v.FechaPasoPorElPC, v.IdUsuarioMarcaPasoPC,
                    v.ObservacionPasoPorElPC,
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
                v.FechaRegistro, v.FechaMarcaDiaD, ISNULL(v.IdUsuarioMarcaDiaD, 0), v.Sexo,
                v.IdRecinto, v.RecintoVotacion, v.Distrito, v.Departamento, v.NroMesa, v.OrdenMesa, v.IdRecintoOk,
                ISNULL(v.PasoPorElPC, 0), v.FechaPasoPorElPC, v.IdUsuarioMarcaPasoPC,
                v.ObservacionPasoPorElPC,
                0
            FROM dbo.TB_Votante v WITH (INDEX(IX_TB_Votante_IdRecinto), NOLOCK)
            WHERE v.IdRecinto = @IdRecintoLegado
              AND (@NroMesa IS NULL OR v.NroMesa = @NroMesa)
              AND (v.Nombres LIKE '%' + @Texto + '%' OR v.Apellidos LIKE '%' + @Texto + '%')
            ORDER BY v.NroMesa ASC, v.OrdenMesa ASC
            OPTION (RECOMPILE);
        END;

        IF NOT EXISTS (SELECT 1 FROM #Resultados) AND @Texto <> '' AND @EsNumero = 1
        BEGIN
            INSERT INTO #Resultados
            SELECT TOP 5
                v.IdVotante, v.Nombres, v.Apellidos,
                LTRIM(RTRIM(ISNULL(v.Nombres, ''))) + ' ' + LTRIM(RTRIM(ISNULL(v.Apellidos, ''))),
                v.CI, v.EstadoRegistro, ISNULL(v.EstadoDiaD, 'PENDIENTE'),
                v.FechaRegistro, v.FechaMarcaDiaD, ISNULL(v.IdUsuarioMarcaDiaD, 0), v.Sexo,
                v.IdRecinto, v.RecintoVotacion, v.Distrito, v.Departamento, v.NroMesa, v.OrdenMesa, v.IdRecintoOk,
                ISNULL(v.PasoPorElPC, 0), v.FechaPasoPorElPC, v.IdUsuarioMarcaPasoPC,
                v.ObservacionPasoPorElPC,
                1
            FROM dbo.TB_Votante v WITH (INDEX(IX_TB_Votante_CI), NOLOCK)
            WHERE v.CI = @Texto
            OPTION (RECOMPILE);
        END;
    END
    ELSE IF (@IdTerritorio IS NOT NULL)
    BEGIN
        DECLARE @RecintosTerritorio TABLE (IdRecintoGuid VARCHAR(50) COLLATE Modern_Spanish_CI_AS PRIMARY KEY);
        
        INSERT INTO @RecintosTerritorio (IdRecintoGuid)
        SELECT DISTINCT CAST(r.IdRecinto AS VARCHAR(50))
        FROM dbo.TB_Recinto r WITH (NOLOCK)
        LEFT JOIN dbo.Territorio t WITH (NOLOCK) ON t.IdTerritorio = r.IdMunicipio
        WHERE r.IdRecinto IS NOT NULL 
          AND (r.IdMunicipio = @IdTerritorio OR t.IdTerritorioPadre = @IdTerritorio);

        IF (@Texto = '')
        BEGIN
            INSERT INTO #Resultados
            SELECT TOP 300
                v.IdVotante, v.Nombres, v.Apellidos,
                LTRIM(RTRIM(ISNULL(v.Nombres, ''))) + ' ' + LTRIM(RTRIM(ISNULL(v.Apellidos, ''))),
                v.CI, v.EstadoRegistro, ISNULL(v.EstadoDiaD, 'PENDIENTE'),
                v.FechaRegistro, v.FechaMarcaDiaD, ISNULL(v.IdUsuarioMarcaDiaD, 0), v.Sexo,
                v.IdRecinto, v.RecintoVotacion, v.Distrito, v.Departamento, v.NroMesa, v.OrdenMesa, v.IdRecintoOk,
                ISNULL(v.PasoPorElPC, 0), v.FechaPasoPorElPC, v.IdUsuarioMarcaPasoPC,
                v.ObservacionPasoPorElPC,
                0
            FROM @RecintosTerritorio rec
            CROSS APPLY (
                SELECT TOP 100
                    v.IdVotante, v.Nombres, v.Apellidos, v.CI, v.EstadoRegistro, v.EstadoDiaD,
                    v.FechaRegistro, v.FechaMarcaDiaD, v.IdUsuarioMarcaDiaD,
                    v.Sexo, v.IdRecinto, v.RecintoVotacion, v.Distrito, v.Departamento, v.NroMesa, v.OrdenMesa, v.IdRecintoOk,
                    v.PasoPorElPC, v.FechaPasoPorElPC, v.IdUsuarioMarcaPasoPC, v.ObservacionPasoPorElPC
                FROM dbo.TB_Votante v WITH (INDEX(IX_TB_Votante_IdRecintoOk), NOLOCK)
                WHERE v.IdRecintoOk = rec.IdRecintoGuid
                  AND (@NroMesa IS NULL OR v.NroMesa = @NroMesa)
                ORDER BY v.NroMesa ASC, v.OrdenMesa ASC
            ) v
            ORDER BY v.RecintoVotacion ASC, TRY_CAST(v.NroMesa AS INT) ASC, v.OrdenMesa ASC
            OPTION (RECOMPILE);
        END
        ELSE IF (@EsNumero = 1)
        BEGIN
            INSERT INTO #Resultados
            SELECT TOP 50
                v.IdVotante, v.Nombres, v.Apellidos,
                LTRIM(RTRIM(ISNULL(v.Nombres, ''))) + ' ' + LTRIM(RTRIM(ISNULL(v.Apellidos, ''))),
                v.CI, v.EstadoRegistro, ISNULL(v.EstadoDiaD, 'PENDIENTE'),
                v.FechaRegistro, v.FechaMarcaDiaD, ISNULL(v.IdUsuarioMarcaDiaD, 0), v.Sexo,
                v.IdRecinto, v.RecintoVotacion, v.Distrito, v.Departamento, v.NroMesa, v.OrdenMesa, v.IdRecintoOk,
                ISNULL(v.PasoPorElPC, 0), v.FechaPasoPorElPC, v.IdUsuarioMarcaPasoPC,
                v.ObservacionPasoPorElPC,
                CASE WHEN rec.IdRecintoGuid IS NOT NULL THEN 0 ELSE 1 END
            FROM dbo.TB_Votante v WITH (INDEX(IX_TB_Votante_CI), NOLOCK)
            LEFT JOIN @RecintosTerritorio rec ON rec.IdRecintoGuid = v.IdRecintoOk
            WHERE v.CI = @Texto OR v.CI LIKE @Texto + '%'
            ORDER BY (CASE WHEN rec.IdRecintoGuid IS NOT NULL THEN 0 ELSE 1 END) ASC, v.CI ASC
            OPTION (RECOMPILE);
        END
        ELSE
        BEGIN
            INSERT INTO #Resultados
            SELECT TOP 100
                v.IdVotante, v.Nombres, v.Apellidos,
                LTRIM(RTRIM(ISNULL(v.Nombres, ''))) + ' ' + LTRIM(RTRIM(ISNULL(v.Apellidos, ''))),
                v.CI, v.EstadoRegistro, ISNULL(v.EstadoDiaD, 'PENDIENTE'),
                v.FechaRegistro, v.FechaMarcaDiaD, ISNULL(v.IdUsuarioMarcaDiaD, 0), v.Sexo,
                v.IdRecinto, v.RecintoVotacion, v.Distrito, v.Departamento, v.NroMesa, v.OrdenMesa, v.IdRecintoOk,
                ISNULL(v.PasoPorElPC, 0), v.FechaPasoPorElPC, v.IdUsuarioMarcaPasoPC,
                v.ObservacionPasoPorElPC,
                0
            FROM @RecintosTerritorio rec
            CROSS APPLY (
                SELECT TOP 50
                    v.IdVotante, v.Nombres, v.Apellidos, v.CI, v.EstadoRegistro, v.EstadoDiaD,
                    v.FechaRegistro, v.FechaMarcaDiaD, v.IdUsuarioMarcaDiaD,
                    v.Sexo, v.IdRecinto, v.RecintoVotacion, v.Distrito, v.Departamento, v.NroMesa, v.OrdenMesa, v.IdRecintoOk,
                    v.PasoPorElPC, v.FechaPasoPorElPC, v.IdUsuarioMarcaPasoPC, v.ObservacionPasoPorElPC
                FROM dbo.TB_Votante v WITH (INDEX(IX_TB_Votante_IdRecintoOk), NOLOCK)
                WHERE v.IdRecintoOk = rec.IdRecintoGuid
                  AND (v.Nombres LIKE '%' + @Texto + '%' OR v.Apellidos LIKE '%' + @Texto + '%')
                  AND (@NroMesa IS NULL OR v.NroMesa = @NroMesa)
            ) v
            ORDER BY v.RecintoVotacion ASC, TRY_CAST(v.NroMesa AS INT) ASC, v.OrdenMesa ASC
            OPTION (RECOMPILE);
        END;
    END
    ELSE
    BEGIN
        IF (@Texto = '')
        BEGIN
            INSERT INTO #Resultados
            SELECT TOP 100
                v.IdVotante, v.Nombres, v.Apellidos,
                LTRIM(RTRIM(ISNULL(v.Nombres, ''))) + ' ' + LTRIM(RTRIM(ISNULL(v.Apellidos, ''))),
                v.CI, v.EstadoRegistro, ISNULL(v.EstadoDiaD, 'PENDIENTE'),
                v.FechaRegistro, v.FechaMarcaDiaD, ISNULL(v.IdUsuarioMarcaDiaD, 0), v.Sexo,
                v.IdRecinto, v.RecintoVotacion, v.Distrito, v.Departamento, v.NroMesa, v.OrdenMesa, v.IdRecintoOk,
                ISNULL(v.PasoPorElPC, 0), v.FechaPasoPorElPC, v.IdUsuarioMarcaPasoPC,
                v.ObservacionPasoPorElPC,
                0
            FROM dbo.TB_Votante v WITH (INDEX(IX_TB_Votante_CI), NOLOCK)
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
                v.FechaRegistro, v.FechaMarcaDiaD, ISNULL(v.IdUsuarioMarcaDiaD, 0), v.Sexo,
                v.IdRecinto, v.RecintoVotacion, v.Distrito, v.Departamento, v.NroMesa, v.OrdenMesa, v.IdRecintoOk,
                ISNULL(v.PasoPorElPC, 0), v.FechaPasoPorElPC, v.IdUsuarioMarcaPasoPC,
                v.ObservacionPasoPorElPC,
                0
            FROM dbo.TB_Votante v WITH (INDEX(IX_TB_Votante_CI), NOLOCK)
            WHERE v.CI = @Texto OR v.CI LIKE @Texto + '%'
            ORDER BY v.CI ASC
            OPTION (RECOMPILE);
        END
        ELSE
        BEGIN
            INSERT INTO #Resultados
            SELECT TOP 50
                v.IdVotante, v.Nombres, v.Apellidos,
                LTRIM(RTRIM(ISNULL(v.Nombres, ''))) + ' ' + LTRIM(RTRIM(ISNULL(v.Apellidos, ''))),
                v.CI, v.EstadoRegistro, ISNULL(v.EstadoDiaD, 'PENDIENTE'),
                v.FechaRegistro, v.FechaMarcaDiaD, ISNULL(v.IdUsuarioMarcaDiaD, 0), v.Sexo,
                v.IdRecinto, v.RecintoVotacion, v.Distrito, v.Departamento, v.NroMesa, v.OrdenMesa, v.IdRecintoOk,
                ISNULL(v.PasoPorElPC, 0), v.FechaPasoPorElPC, v.IdUsuarioMarcaPasoPC,
                v.ObservacionPasoPorElPC,
                0
            FROM dbo.TB_Votante v WITH (NOLOCK)
            WHERE (v.Nombres LIKE '%' + @Texto + '%' OR v.Apellidos LIKE '%' + @Texto + '%')
            OPTION (RECOMPILE);
        END;
    END;

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
        u.ObservacionPasoPorElPC AS ObservacionPasoPorElPC,
        u.ObservacionPasoPorElPC AS ObservacionPC,
        u.ObservacionPasoPorElPC AS Observacion,
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
        u.RecintoVotacion ASC,
        TRY_CAST(u.NroMesa AS INT) ASC, 
        u.NroOrden ASC
    OPTION (RECOMPILE);

    DROP TABLE #Resultados;
END;
GO
