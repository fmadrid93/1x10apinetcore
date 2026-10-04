-- =========================================================================
-- Procedimiento: PA_LIMPIAR_MARCAS_DIAD
-- Descripción: Limpia las marcas del Día D (Votos marcados, Pasó por el PC, Combustible)
--              en TB_Votante, PersonaMovilizada y TB_DiaDHistorial.
--              Soporta filtro por @IdTerritorio (Municipio o Departamento) o limpieza global.
-- =========================================================================

CREATE OR ALTER PROCEDURE dbo.PA_LIMPIAR_MARCAS_DIAD
    @LimpiarVoto BIT = 1,
    @LimpiarGasolina BIT = 1,
    @LimpiarPasoPC BIT = 1,
    @IdTerritorio INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @TotalAfectadas INT = 0;

    -- =========================================================================
    -- 1. LIMPIAR VOTOS (TB_Votante, PersonaMovilizada, TB_DiaDHistorial)
    -- =========================================================================
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

        -- PersonaMovilizada matching por CI
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

        IF COL_LENGTH('dbo.PersonaMovilizada', 'YaVoto') IS NOT NULL
        BEGIN
            DECLARE @sqlYaVoto NVARCHAR(MAX) = '
                UPDATE pm SET pm.YaVoto = 0
                FROM dbo.PersonaMovilizada pm
                LEFT JOIN dbo.Usuario uMov ON uMov.IdUsuario = pm.IdUsuarioMovilizador
                WHERE (@IdTerritorio IS NULL OR pm.IdTerritorio = @IdTerritorio OR uMov.IdTerritorio = @IdTerritorio) AND ISNULL(pm.YaVoto, 0) = 1;';
            EXEC sp_executesql @sqlYaVoto, N'@IdTerritorio INT', @IdTerritorio;
        END

        IF COL_LENGTH('dbo.PersonaMovilizada', 'FechaVoto') IS NOT NULL
        BEGIN
            DECLARE @sqlFechaVoto NVARCHAR(MAX) = '
                UPDATE pm SET pm.FechaVoto = NULL
                FROM dbo.PersonaMovilizada pm
                LEFT JOIN dbo.Usuario uMov ON uMov.IdUsuario = pm.IdUsuarioMovilizador
                WHERE (@IdTerritorio IS NULL OR pm.IdTerritorio = @IdTerritorio OR uMov.IdTerritorio = @IdTerritorio) AND pm.FechaVoto IS NOT NULL;';
            EXEC sp_executesql @sqlFechaVoto, N'@IdTerritorio INT', @IdTerritorio;
        END

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

    -- =========================================================================
    -- 2. LIMPIAR GASOLINA / COMBUSTIBLE
    -- =========================================================================
    IF (@LimpiarGasolina = 1)
    BEGIN
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

    -- =========================================================================
    -- 3. LIMPIAR PASÓ POR EL PC
    -- =========================================================================
    IF (@LimpiarPasoPC = 1)
    BEGIN
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

    SELECT @TotalAfectadas AS TotalAfectadas;
END;
GO
