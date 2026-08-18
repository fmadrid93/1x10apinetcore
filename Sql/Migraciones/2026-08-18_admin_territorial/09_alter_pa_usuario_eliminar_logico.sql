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
*/

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
