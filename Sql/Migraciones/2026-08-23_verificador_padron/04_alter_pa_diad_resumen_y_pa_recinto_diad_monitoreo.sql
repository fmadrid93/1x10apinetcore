/*
    Migración: 04_alter_pa_diad_resumen_y_pa_recinto_diad_monitoreo.sql
    1. Corrige jerarquía territorial para Itapúa y Fram.
    2. Actualiza PA_DASHBOARD_DIAD_RESUMEN con soporte para filtro por IdUsuario/Estructura y cálculo correcto de reportes.
    3. Crea/Actualiza PA_RECINTO_DIAD_MONITOREO para conteos y listados de las 3 pestañas:
       - Faltan Votar (Padrón)
       - Votaron No Registrados (Espontáneos)
       - Registrados 1x10 que Faltan Votar
*/

-- 1. Corrección de jerarquía territorial
UPDATE Territorio 
SET IdTerritorioPadre = NULL, TipoTerritorio = 'DEPARTAMENTO'
WHERE IdTerritorio = 1019;

UPDATE Territorio
SET IdTerritorioPadre = 1019, TipoTerritorio = 'MUNICIPIO'
WHERE IdTerritorio = 1237;
GO

-- 2. PA_DASHBOARD_DIAD_RESUMEN
CREATE OR ALTER PROCEDURE [dbo].[PA_DASHBOARD_DIAD_RESUMEN]
    @IdUsuario VARCHAR(50) = NULL,
    @HoraInicio INT = NULL,
    @HoraFin INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @IdAdmin INT = TRY_CAST(@IdUsuario AS INT);
    DECLARE @IdTerritorioAdmin INT = NULL;
    DECLARE @IdRolAdmin INT = NULL;

    IF @IdAdmin IS NOT NULL AND @IdAdmin > 0
    BEGIN
        SELECT @IdTerritorioAdmin = u.IdTerritorio, @IdRolAdmin = u.IdRol
        FROM Usuario u WITH (NOLOCK)
        WHERE u.IdUsuario = @IdAdmin;
    END

    ------------------------------------------------------------
    -- 1. RESUMEN / KPIS
    ------------------------------------------------------------
    ;WITH ArbolTerritorios AS (
        SELECT IdTerritorio
        FROM Territorio WITH (NOLOCK)
        WHERE IdTerritorio = @IdTerritorioAdmin
        UNION ALL
        SELECT t.IdTerritorio
        FROM Territorio t WITH (NOLOCK)
        INNER JOIN ArbolTerritorios a ON t.IdTerritorioPadre = a.IdTerritorio
    ),
    GerentesDelAdmin AS (
        SELECT u.IdUsuario
        FROM Usuario u WITH (NOLOCK)
        WHERE u.IdRol = 2
          AND (u.Activo IS NULL OR u.Activo = 1)
          AND (
              (@IdAdmin IS NULL OR @IdAdmin = 0)
              OR (@IdTerritorioAdmin IS NOT NULL AND u.IdTerritorio IN (SELECT IdTerritorio FROM ArbolTerritorios))
              OR u.IdUsuarioSupervisor = @IdAdmin
              OR u.IdUsuarioCreate = @IdAdmin
          )
    ),
    MovilizadoresEstructura AS (
        SELECT u.IdUsuario
        FROM Usuario u WITH (NOLOCK)
        WHERE u.IdRol = 3
          AND (u.Activo IS NULL OR u.Activo = 1)
          AND (
              (@IdAdmin IS NULL OR @IdAdmin = 0)
              OR (@IdTerritorioAdmin IS NOT NULL AND u.IdTerritorio IN (SELECT IdTerritorio FROM ArbolTerritorios))
              OR u.IdUsuarioSupervisor IN (SELECT IdUsuario FROM GerentesDelAdmin)
              OR u.IdUsuarioSupervisor = @IdAdmin
              OR u.IdUsuarioCreate = @IdAdmin
          )
    )
    SELECT
        COUNT(DISTINCT u.IdUsuario) AS TotalMovilizadores,
        COUNT(DISTINCT CASE WHEN p.EstadoDiaD IS NOT NULL AND p.EstadoDiaD NOT IN ('PENDIENTE', '') AND p.FechaMarcaDiaD IS NOT NULL THEN u.IdUsuario END) AS MovilizadoresReportando,
        ISNULL(SUM(CASE WHEN p.EstadoDiaD IS NOT NULL AND p.EstadoDiaD NOT IN ('PENDIENTE', '') AND p.FechaMarcaDiaD IS NOT NULL THEN 1 ELSE 0 END), 0) AS TotalReportes,
        ISNULL(SUM(CASE WHEN p.EstadoDiaD = 'YA_VOTO' THEN 1 ELSE 0 END), 0) AS TotalYaVoto,
        ISNULL(SUM(CASE WHEN p.EstadoDiaD = 'NO_CONTACTADO' THEN 1 ELSE 0 END), 0) AS TotalNoContactado,
        COUNT(p.IdPersonaMovilizada) AS TotalRegistrados
    FROM Usuario u WITH (NOLOCK)
    INNER JOIN MovilizadoresEstructura me ON u.IdUsuario = me.IdUsuario
    INNER JOIN Rol r WITH (NOLOCK) ON r.IdRol = u.IdRol
    LEFT JOIN PersonaMovilizada p WITH (NOLOCK)
        ON p.IdUsuarioMovilizador = u.IdUsuario
       AND (p.Activo IS NULL OR p.Activo = 1)
       AND (
            p.FechaMarcaDiaD IS NULL
            OR (
                (@HoraInicio IS NULL OR DATEPART(HOUR, p.FechaMarcaDiaD) >= @HoraInicio)
                AND (@HoraFin IS NULL OR DATEPART(HOUR, p.FechaMarcaDiaD) <= @HoraFin)
            )
       )
    WHERE r.Nombre = 'MOVILIZADOR'
      AND (u.Activo IS NULL OR u.Activo = 1);

    ------------------------------------------------------------
    -- 2. CURVA DE AVANCE POR HORA
    ------------------------------------------------------------
    ;WITH ArbolTerritorios AS (
        SELECT IdTerritorio
        FROM Territorio WITH (NOLOCK)
        WHERE IdTerritorio = @IdTerritorioAdmin
        UNION ALL
        SELECT t.IdTerritorio
        FROM Territorio t WITH (NOLOCK)
        INNER JOIN ArbolTerritorios a ON t.IdTerritorioPadre = a.IdTerritorio
    ),
    GerentesDelAdmin AS (
        SELECT u.IdUsuario
        FROM Usuario u WITH (NOLOCK)
        WHERE u.IdRol = 2
          AND (u.Activo IS NULL OR u.Activo = 1)
          AND (
              (@IdAdmin IS NULL OR @IdAdmin = 0)
              OR (@IdTerritorioAdmin IS NOT NULL AND u.IdTerritorio IN (SELECT IdTerritorio FROM ArbolTerritorios))
              OR u.IdUsuarioSupervisor = @IdAdmin
              OR u.IdUsuarioCreate = @IdAdmin
          )
    ),
    MovilizadoresEstructura AS (
        SELECT u.IdUsuario
        FROM Usuario u WITH (NOLOCK)
        WHERE u.IdRol = 3
          AND (u.Activo IS NULL OR u.Activo = 1)
          AND (
              (@IdAdmin IS NULL OR @IdAdmin = 0)
              OR (@IdTerritorioAdmin IS NOT NULL AND u.IdTerritorio IN (SELECT IdTerritorio FROM ArbolTerritorios))
              OR u.IdUsuarioSupervisor IN (SELECT IdUsuario FROM GerentesDelAdmin)
              OR u.IdUsuarioSupervisor = @IdAdmin
              OR u.IdUsuarioCreate = @IdAdmin
          )
    )
    SELECT 
        DATEPART(HOUR, p.FechaMarcaDiaD) AS Hora,
        COUNT(*) AS Total
    FROM PersonaMovilizada p WITH (NOLOCK)
    INNER JOIN MovilizadoresEstructura me ON p.IdUsuarioMovilizador = me.IdUsuario
    WHERE (p.Activo IS NULL OR p.Activo = 1)
      AND p.EstadoDiaD = 'YA_VOTO'
      AND p.FechaMarcaDiaD IS NOT NULL
      AND (@HoraInicio IS NULL OR DATEPART(HOUR, p.FechaMarcaDiaD) >= @HoraInicio)
      AND (@HoraFin IS NULL OR DATEPART(HOUR, p.FechaMarcaDiaD) <= @HoraFin)
    GROUP BY DATEPART(HOUR, p.FechaMarcaDiaD)
    ORDER BY Hora;

    ------------------------------------------------------------
    -- 3. VELOCIDAD DE VOTACION CADA 30 MIN
    ------------------------------------------------------------
    ;WITH ArbolTerritorios AS (
        SELECT IdTerritorio
        FROM Territorio WITH (NOLOCK)
        WHERE IdTerritorio = @IdTerritorioAdmin
        UNION ALL
        SELECT t.IdTerritorio
        FROM Territorio t WITH (NOLOCK)
        INNER JOIN ArbolTerritorios a ON t.IdTerritorioPadre = a.IdTerritorio
    ),
    GerentesDelAdmin AS (
        SELECT u.IdUsuario
        FROM Usuario u WITH (NOLOCK)
        WHERE u.IdRol = 2
          AND (u.Activo IS NULL OR u.Activo = 1)
          AND (
              (@IdAdmin IS NULL OR @IdAdmin = 0)
              OR (@IdTerritorioAdmin IS NOT NULL AND u.IdTerritorio IN (SELECT IdTerritorio FROM ArbolTerritorios))
              OR u.IdUsuarioSupervisor = @IdAdmin
              OR u.IdUsuarioCreate = @IdAdmin
          )
    ),
    MovilizadoresEstructura AS (
        SELECT u.IdUsuario
        FROM Usuario u WITH (NOLOCK)
        WHERE u.IdRol = 3
          AND (u.Activo IS NULL OR u.Activo = 1)
          AND (
              (@IdAdmin IS NULL OR @IdAdmin = 0)
              OR (@IdTerritorioAdmin IS NOT NULL AND u.IdTerritorio IN (SELECT IdTerritorio FROM ArbolTerritorios))
              OR u.IdUsuarioSupervisor IN (SELECT IdUsuario FROM GerentesDelAdmin)
              OR u.IdUsuarioSupervisor = @IdAdmin
              OR u.IdUsuarioCreate = @IdAdmin
          )
    )
    SELECT 
        DATEPART(HOUR, p.FechaMarcaDiaD) AS Hora,
        DATEPART(MINUTE, p.FechaMarcaDiaD) / 30 AS Bloque,
        COUNT(*) AS Total
    FROM PersonaMovilizada p WITH (NOLOCK)
    INNER JOIN MovilizadoresEstructura me ON p.IdUsuarioMovilizador = me.IdUsuario
    WHERE (p.Activo IS NULL OR p.Activo = 1)
      AND p.EstadoDiaD = 'YA_VOTO'
      AND p.FechaMarcaDiaD IS NOT NULL
      AND (@HoraInicio IS NULL OR DATEPART(HOUR, p.FechaMarcaDiaD) >= @HoraInicio)
      AND (@HoraFin IS NULL OR DATEPART(HOUR, p.FechaMarcaDiaD) <= @HoraFin)
    GROUP BY 
        DATEPART(HOUR, p.FechaMarcaDiaD),
        DATEPART(MINUTE, p.FechaMarcaDiaD) / 30
    ORDER BY Hora, Bloque;

    ------------------------------------------------------------
    -- 4. BRECHA VS META
    ------------------------------------------------------------
    ;WITH ArbolTerritorios AS (
        SELECT IdTerritorio
        FROM Territorio WITH (NOLOCK)
        WHERE IdTerritorio = @IdTerritorioAdmin
        UNION ALL
        SELECT t.IdTerritorio
        FROM Territorio t WITH (NOLOCK)
        INNER JOIN ArbolTerritorios a ON t.IdTerritorioPadre = a.IdTerritorio
    ),
    GerentesDelAdmin AS (
        SELECT u.IdUsuario
        FROM Usuario u WITH (NOLOCK)
        WHERE u.IdRol = 2
          AND (u.Activo IS NULL OR u.Activo = 1)
          AND (
              (@IdAdmin IS NULL OR @IdAdmin = 0)
              OR (@IdTerritorioAdmin IS NOT NULL AND u.IdTerritorio IN (SELECT IdTerritorio FROM ArbolTerritorios))
              OR u.IdUsuarioSupervisor = @IdAdmin
              OR u.IdUsuarioCreate = @IdAdmin
          )
    ),
    MovilizadoresEstructura AS (
        SELECT u.IdUsuario
        FROM Usuario u WITH (NOLOCK)
        WHERE u.IdRol = 3
          AND (u.Activo IS NULL OR u.Activo = 1)
          AND (
              (@IdAdmin IS NULL OR @IdAdmin = 0)
              OR (@IdTerritorioAdmin IS NOT NULL AND u.IdTerritorio IN (SELECT IdTerritorio FROM ArbolTerritorios))
              OR u.IdUsuarioSupervisor IN (SELECT IdUsuario FROM GerentesDelAdmin)
              OR u.IdUsuarioSupervisor = @IdAdmin
              OR u.IdUsuarioCreate = @IdAdmin
          )
    )
    SELECT 
        ISNULL((
            SELECT SUM(ISNULL(mm.MetaObjetivo, 10))
            FROM MovilizadoresEstructura me
            LEFT JOIN MovilizadorMeta mm WITH (NOLOCK) ON mm.IdUsuarioMovilizador = me.IdUsuario
        ), 0) AS MetaTotal,
        ISNULL((
            SELECT COUNT(1)
            FROM PersonaMovilizada p WITH (NOLOCK)
            INNER JOIN MovilizadoresEstructura me ON p.IdUsuarioMovilizador = me.IdUsuario
            WHERE (p.Activo IS NULL OR p.Activo = 1)
              AND p.EstadoDiaD = 'YA_VOTO'
              AND p.FechaMarcaDiaD IS NOT NULL
              AND (@HoraInicio IS NULL OR DATEPART(HOUR, p.FechaMarcaDiaD) >= @HoraInicio)
              AND (@HoraFin IS NULL OR DATEPART(HOUR, p.FechaMarcaDiaD) <= @HoraFin)
        ), 0) AS Real;
END;
GO