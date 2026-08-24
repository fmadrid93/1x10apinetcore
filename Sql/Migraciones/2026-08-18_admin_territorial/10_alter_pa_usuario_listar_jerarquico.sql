/*
    10. Extiende pa_usuario_listar para soportar visibilidad jerarquica
    y contar personas registradas por cada movilizador.
*/

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE pa_usuario_listar
    @IdRol INT = NULL,
    @IdTerritorio INT = NULL,
    @IdUsuarioSupervisor INT = NULL,
    @SoloActivos BIT = 1,
    @IdUsuarioCreate INT = NULL,
    @IdsCreador VARCHAR(MAX) = NULL,
    @IdSupervisorPropio INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.IdUsuario,
        u.IdRol,
        r.Nombre AS Rol,
        u.IdTerritorio,
        t.Nombre AS Territorio,
        u.IdUsuarioSupervisor,
        s.NombreCompleto AS Supervisor,
        u.Usuario,
        u.NombreCompleto,
        u.CI,
        u.Celular,
        u.Email,
        u.Activo,
        u.FechaCreate,
        u.FechaUpdate,
        ISNULL((
            SELECT COUNT(1)
            FROM PersonaMovilizada pm WITH (NOLOCK)
            WHERE pm.IdUsuarioMovilizador = u.IdUsuario
              AND (pm.Activo IS NULL OR pm.Activo = 1)
        ), 0) AS TotalPersonas
    FROM Usuario u
    INNER JOIN Rol r ON r.IdRol = u.IdRol
    LEFT JOIN Territorio t ON t.IdTerritorio = u.IdTerritorio
    LEFT JOIN Usuario s ON s.IdUsuario = u.IdUsuarioSupervisor
    WHERE (@IdRol IS NULL OR u.IdRol = @IdRol)
      AND (@IdTerritorio IS NULL OR u.IdTerritorio = @IdTerritorio)
      AND (@IdUsuarioSupervisor IS NULL OR u.IdUsuarioSupervisor = @IdUsuarioSupervisor)
      AND (@SoloActivos = 0 OR u.Activo = 1)
      AND (@IdUsuarioCreate IS NULL OR u.IdUsuarioCreate = @IdUsuarioCreate)
      AND (
            (@IdsCreador IS NULL AND @IdSupervisorPropio IS NULL)
            OR (@IdsCreador IS NOT NULL AND u.IdUsuarioCreate IN (SELECT CAST(value AS INT) FROM STRING_SPLIT(@IdsCreador, ',')))
            OR (@IdSupervisorPropio IS NOT NULL AND u.IdUsuarioSupervisor = @IdSupervisorPropio)
          )
    ORDER BY r.Nombre, u.NombreCompleto;
END
GO
