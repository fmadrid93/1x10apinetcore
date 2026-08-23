-- =========================================================================
-- Procedimiento: PA_VOTANTE_BUSCAR_PADRON_GLOBAL
-- Descripción: Búsqueda indexada en el padrón electoral con coincidencia
--              exacta por IdRecinto (TRIM) y Mesa (TRIM), además de
--              búsqueda flexible por CI, nombres y apellidos.
-- =========================================================================

CREATE OR ALTER PROCEDURE dbo.PA_VOTANTE_BUSCAR_PADRON_GLOBAL
    @Texto VARCHAR(100) = '',
    @IdRecinto VARCHAR(150) = NULL,
    @NroMesa VARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SET @Texto = LTRIM(RTRIM(ISNULL(@Texto, '')));
    SET @IdRecinto = LTRIM(RTRIM(NULLIF(@IdRecinto, '')));
    SET @NroMesa = LTRIM(RTRIM(NULLIF(@NroMesa, '')));

    SELECT TOP 100
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
    WHERE 
        (@IdRecinto IS NULL OR LTRIM(RTRIM(ISNULL(CAST(v.IdRecinto AS VARCHAR(150)), ''))) = @IdRecinto)
        AND (@NroMesa IS NULL OR LTRIM(RTRIM(ISNULL(v.NroMesa, ISNULL(v.Mesa, '')))) = @NroMesa)
        AND (
            @Texto = ''
            OR v.CI = @Texto
            OR v.CI LIKE @Texto + '%'
            OR v.Apellidos LIKE '%' + @Texto + '%'
            OR v.Nombres LIKE '%' + @Texto + '%'
            OR (v.Nombres + ' ' + v.Apellidos) LIKE '%' + @Texto + '%'
        )
    ORDER BY v.IdVotante ASC;
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
