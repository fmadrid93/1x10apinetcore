/*
    09. Actualiza pa_usuario_eliminar_logico para grabar IdUsuarioDelete,
    FechaDelete y Motivo en la baja logica (§23.3 del manual).

    Antes esta baja reusaba FechaUpdate (incorrecto: una baja no es una
    edicion). Ahora usa FechaDelete/IdUsuarioDelete propios y ya no toca
    IdUsuarioUpdate/FechaUpdate, tal como indica el manual:
    "No se deben modificar: IdUsuarioCreate, FechaCreate, IdUsuarioUpdate,
    FechaUpdate" durante una baja.

    @IdUsuarioDelete y @Motivo son opcionales para no romper llamadores
    existentes.

    IMPORTANTE: Usuario tiene un indice unico FILTRADO
    (UX_Usuario_Usuario, WHERE Usuario IS NOT NULL). Cualquier UPDATE
    contra esta tabla exige QUOTED_IDENTIFIER ON en la sesion que crea el
    procedure (si no, SQL Server da error "SET options have incorrect
    settings" al ejecutarlo). Por eso los SET de abajo son obligatorios.
*/

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE pa_usuario_eliminar_logico
    @IdUsuario INT,
    @IdUsuarioDelete INT = NULL,
    @Motivo NVARCHAR(300) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Usuario
    SET
        Activo = 0,
        FechaDelete = GETDATE(),
        IdUsuarioDelete = @IdUsuarioDelete,
        Motivo = NULLIF(@Motivo, '')
    WHERE IdUsuario = @IdUsuario;

    SELECT 1 AS Exito, 'Usuario desactivado correctamente' AS Mensaje;
END
GO
