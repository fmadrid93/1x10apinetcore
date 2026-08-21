/*
    05. Agrega IdUsuarioCreate al SELECT de pa_usuario_obtener_por_id.

    El Service necesita saber quién creó al usuario objetivo para poder
    validar ownership antes de Actualizar/CambiarClave/EliminarLogico
    cuando quien llama es un admin territorial (patrón de autorización por
    recurso, ver Manual_Estandares_CSharp_SQLServer_v8.md §40).

    Se preserva el resto de la lógica original.

    Nota: solo lectura (SELECT); se agrega QUOTED_IDENTIFIER ON por
    consistencia con el resto de procedures de Usuario.
*/

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE pa_usuario_obtener_por_id
    @IdUsuario INT
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
        u.ClaveHash,
        u.NombreCompleto,
        u.CI,
        u.Celular,
        u.Email,
        u.Activo,
        u.FechaCreate,
        u.FechaUpdate,
        u.IdUsuarioCreate
    FROM Usuario u
    INNER JOIN Rol r ON r.IdRol = u.IdRol
    LEFT JOIN Territorio t ON t.IdTerritorio = u.IdTerritorio
    LEFT JOIN Usuario s ON s.IdUsuario = u.IdUsuarioSupervisor
    WHERE u.IdUsuario = @IdUsuario;
END
GO
