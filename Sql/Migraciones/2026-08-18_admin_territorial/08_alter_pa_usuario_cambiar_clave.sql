/*
    08. Actualiza pa_usuario_cambiar_clave para grabar IdUsuarioUpdate,
    FechaUpdate y Motivo (un cambio de clave tambien es una edicion, §23.2).

    @IdUsuarioUpdate y @Motivo son opcionales para no romper llamadores
    existentes.
*/

CREATE OR ALTER PROCEDURE pa_usuario_cambiar_clave
    @IdUsuario INT,
    @ClaveHash VARCHAR(300),
    @IdUsuarioUpdate INT = NULL,
    @Motivo NVARCHAR(300) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Usuario
    SET
        ClaveHash = @ClaveHash,
        FechaUpdate = GETDATE(),
        IdUsuarioUpdate = @IdUsuarioUpdate,
        Motivo = NULLIF(@Motivo, '')
    WHERE IdUsuario = @IdUsuario;

    SELECT 1 AS Exito, 'Clave actualizada correctamente' AS Mensaje;
END
GO
