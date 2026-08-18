/*
    04. Actualiza pa_usuario_listar para poder filtrar por @IdUsuarioCreate.

    @IdUsuarioCreate = NULL -> sin filtro (comportamiento actual, usado por
    super admins).
    @IdUsuarioCreate = <id> -> solo devuelve usuarios creados por ese admin
    (usado por admins territoriales).

    UsuarioController.ReporteExcel y ReportePdf reusan este mismo procedure
    a través de UsuarioService.Listar(), así que heredan el filtro sin
    necesidad de tocar esos endpoints por separado.

    Se preserva el resto de la lógica original (joins a Rol/Territorio/
    supervisor, filtros existentes, orden).
*/

CREATE OR ALTER PROCEDURE pa_usuario_listar
    @IdRol INT = NULL,
    @IdTerritorio INT = NULL,
    @IdUsuarioSupervisor INT = NULL,
    @SoloActivos BIT = 1,
    @IdUsuarioCreate INT = NULL
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
        u.FechaUpdate
    FROM Usuario u
    INNER JOIN Rol r ON r.IdRol = u.IdRol
    LEFT JOIN Territorio t ON t.IdTerritorio = u.IdTerritorio
    LEFT JOIN Usuario s ON s.IdUsuario = u.IdUsuarioSupervisor
    WHERE (@IdRol IS NULL OR u.IdRol = @IdRol)
      AND (@IdTerritorio IS NULL OR u.IdTerritorio = @IdTerritorio)
      AND (@IdUsuarioSupervisor IS NULL OR u.IdUsuarioSupervisor = @IdUsuarioSupervisor)
      AND (@SoloActivos = 0 OR u.Activo = 1)
      AND (@IdUsuarioCreate IS NULL OR u.IdUsuarioCreate = @IdUsuarioCreate)
    ORDER BY r.Nombre, u.NombreCompleto;
END
GO
