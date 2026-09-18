-- ============================================================================
-- Migración: Stored Procedure para CIs duplicados con alcance jerárquico hacia abajo
-- Archivo: 05_pa_persona_movilizada_ci_duplicados.sql
-- Descripción: Permite a SuperAdmin, Administrador Territorial, Gerente y Movilizador
--              visualizar las cédulas duplicadas en el sistema que tocan su estructura,
--              con desglose de quién registró cada una, gerente y territorio.
-- ============================================================================

CREATE OR ALTER PROCEDURE dbo.pa_persona_movilizada_ci_duplicados
    @IdUsuario INT = NULL,
    @IdRol INT = NULL,
    @IdTerritorio INT = NULL,
    @Texto VARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SET @Texto = LTRIM(RTRIM(NULLIF(@Texto, '')));

    -- 1. Determinar el rol y territorio si no vienen explícitos
    IF (@IdUsuario IS NOT NULL AND @IdUsuario > 0)
    BEGIN
        IF @IdRol IS NULL OR @IdRol = 0
        BEGIN
            SELECT @IdRol = u.IdRol, 
                   @IdTerritorio = ISNULL(@IdTerritorio, u.IdTerritorio)
            FROM Usuario u WITH (NOLOCK)
            WHERE u.IdUsuario = @IdUsuario;
        END
    END

    -- 2. Árbol territorial (para Administradores con territorio)
    ;WITH ArbolTerritorios AS (
        SELECT IdTerritorio
        FROM Territorio WITH (NOLOCK)
        WHERE IdTerritorio = @IdTerritorio
        UNION ALL
        SELECT t.IdTerritorio
        FROM Territorio t WITH (NOLOCK)
        INNER JOIN ArbolTerritorios a ON t.IdTerritorioPadre = a.IdTerritorio
    ),
    -- Gerentes en la estructura del usuario solicitante
    GerentesEstructura AS (
        SELECT u.IdUsuario
        FROM Usuario u WITH (NOLOCK)
        WHERE u.IdRol = 2
          AND (u.Activo IS NULL OR u.Activo = 1)
          AND (
              -- SuperAdmin (Rol 1 sin territorio y sin IdUsuario)
              (@IdRol = 1 AND (@IdTerritorio IS NULL OR @IdTerritorio = 0) AND (@IdUsuario IS NULL OR @IdUsuario = 0))
              -- Admin con territorio o IdUsuario
              OR (@IdRol = 1 AND (@IdTerritorio IS NOT NULL AND u.IdTerritorio IN (SELECT IdTerritorio FROM ArbolTerritorios)))
              OR (@IdRol = 1 AND (@IdUsuario IS NOT NULL AND (u.IdUsuarioSupervisor = @IdUsuario OR u.IdUsuarioCreate = @IdUsuario)))
              -- Gerente (él mismo)
              OR (@IdRol = 2 AND u.IdUsuario = @IdUsuario)
          )
    ),
    -- Movilizadores en la estructura del usuario solicitante
    MovilizadoresEstructura AS (
        SELECT u.IdUsuario
        FROM Usuario u WITH (NOLOCK)
        WHERE u.IdRol = 3
          AND (u.Activo IS NULL OR u.Activo = 1)
          AND (
              -- SuperAdmin
              (@IdRol = 1 AND (@IdTerritorio IS NULL OR @IdTerritorio = 0) AND (@IdUsuario IS NULL OR @IdUsuario = 0))
              -- Admin con territorio o por supervisión
              OR (@IdRol = 1 AND (@IdTerritorio IS NOT NULL AND u.IdTerritorio IN (SELECT IdTerritorio FROM ArbolTerritorios)))
              OR (@IdRol = 1 AND (@IdUsuario IS NOT NULL AND (u.IdUsuarioSupervisor = @IdUsuario OR u.IdUsuarioCreate = @IdUsuario)))
              OR (@IdRol = 1 AND (u.IdUsuarioSupervisor IN (SELECT IdUsuario FROM GerentesEstructura)))
              -- Gerente
              OR (@IdRol = 2 AND (u.IdUsuarioSupervisor = @IdUsuario OR u.IdUsuarioCreate = @IdUsuario OR u.IdUsuarioSupervisor IN (SELECT IdUsuario FROM GerentesEstructura)))
              -- Movilizador
              OR (@IdRol = 3 AND u.IdUsuario = @IdUsuario)
          )
    ),
    -- CIs que están registrados bajo los movilizadores de mi estructura
    CIsEnMiEstructura AS (
        SELECT DISTINCT LTRIM(RTRIM(pm.CI)) AS CI
        FROM PersonaMovilizada pm WITH (NOLOCK)
        INNER JOIN MovilizadoresEstructura me ON pm.IdUsuarioMovilizador = me.IdUsuario
        WHERE (pm.Activo IS NULL OR pm.Activo = 1)
          AND pm.CI IS NOT NULL
          AND LTRIM(RTRIM(pm.CI)) <> ''
    ),
    -- CIs que están duplicados (aparecen 2 o más veces en el sistema)
    CIsDuplicados AS (
        SELECT LTRIM(RTRIM(pm.CI)) AS CI, COUNT(*) AS TotalRepeticiones
        FROM PersonaMovilizada pm WITH (NOLOCK)
        WHERE (pm.Activo IS NULL OR pm.Activo = 1)
          AND pm.CI IS NOT NULL
          AND LTRIM(RTRIM(pm.CI)) IN (SELECT CI FROM CIsEnMiEstructura)
        GROUP BY LTRIM(RTRIM(pm.CI))
        HAVING COUNT(*) > 1
    )
    SELECT 
        pm.IdPersonaMovilizada,
        LTRIM(RTRIM(pm.CI)) AS CI,
        pm.Nombres,
        pm.Apellidos,
        (ISNULL(pm.Nombres, '') + ' ' + ISNULL(pm.Apellidos, '')) AS NombreCompleto,
        pm.Celular,
        pm.RecintoVotacion,
        v.NroMesa,
        v.OrdenMesa AS NroOrden,
        pm.FechaRegistro,
        ISNULL(pm.EstadoDiaD, 'PENDIENTE') AS EstadoDiaD,
        ISNULL(pm.EstadoApoyo, 'PENDIENTE') AS EstadoApoyo,
        pm.IdUsuarioMovilizador AS IdMovilizador,
        ISNULL(m.NombreCompleto, 'Desconocido') AS NombreMovilizador,
        m.Celular AS CelularMovilizador,
        g.IdUsuario AS IdGerente,
        ISNULL(g.NombreCompleto, 'Sin Gerente') AS NombreGerente,
        g.Celular AS CelularGerente,
        ISNULL(pm.IdTerritorio, m.IdTerritorio) AS IdTerritorio,
        ISNULL(tpm.Nombre, tm.Nombre) AS NombreTerritorio,
        cd.TotalRepeticiones,
        CASE WHEN pm.IdUsuarioMovilizador IN (SELECT IdUsuario FROM MovilizadoresEstructura) THEN 1 ELSE 0 END AS EsDeMiEstructura
    FROM PersonaMovilizada pm WITH (NOLOCK)
    INNER JOIN CIsDuplicados cd ON LTRIM(RTRIM(pm.CI)) = cd.CI
    LEFT JOIN Usuario m WITH (NOLOCK) ON m.IdUsuario = pm.IdUsuarioMovilizador
    LEFT JOIN Usuario g WITH (NOLOCK) ON g.IdUsuario = m.IdUsuarioSupervisor
    LEFT JOIN Territorio tpm WITH (NOLOCK) ON tpm.IdTerritorio = pm.IdTerritorio
    LEFT JOIN Territorio tm WITH (NOLOCK) ON tm.IdTerritorio = m.IdTerritorio
    LEFT JOIN TB_Votante v WITH (NOLOCK) ON v.CI = pm.CI
    WHERE (pm.Activo IS NULL OR pm.Activo = 1)
      AND (
          @Texto IS NULL
          OR pm.CI LIKE @Texto + '%'
          OR (pm.Nombres + ' ' + pm.Apellidos) LIKE '%' + @Texto + '%'
          OR m.NombreCompleto LIKE '%' + @Texto + '%'
          OR g.NombreCompleto LIKE '%' + @Texto + '%'
          OR ISNULL(tpm.Nombre, tm.Nombre) LIKE '%' + @Texto + '%'
      )
    ORDER BY cd.TotalRepeticiones DESC, LTRIM(RTRIM(pm.CI)) ASC, pm.FechaRegistro ASC;
END;
GO
