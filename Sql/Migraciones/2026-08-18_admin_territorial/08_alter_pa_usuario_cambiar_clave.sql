/*
    08. Actualiza pa_usuario_cambiar_clave para grabar IdUsuarioUpdate,
    FechaUpdate y Motivo (un cambio de clave tambien es una edicion, §23.2).

    @IdUsuarioUpdate y @Motivo son opcionales para no romper llamadores
    existentes.

    IMPORTANTE: Usuario tiene un indice unico FILTRADO
    (UX_Usuario_Usuario, WHERE Usuario IS NOT NULL). Cualquier UPDATE
    contra esta tabla exige QUOTED_IDENTIFIER ON en la sesion que crea el
    procedure (si no, SQL Server da error "SET options have incorrect
    settings" al ejecutarlo). Por eso los SET de abajo son obligatorios.

    De paso se agrega una validacion de existencia (igual que
    pa_usuario_actualizar): antes no existia, asi que un IdUsuario
    inexistente igual devolvia "Exito = 1".
*/

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE pa_usuario_cambiar_clave
    @IdUsuario INT,
    @ClaveHash VARCHAR(300),
    @IdUsuarioUpdate INT = NULL,
    @Motivo NVARCHAR(300) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM Usuario WHERE IdUsuario = @IdUsuario)
    BEGIN
        SELECT 0 AS Exito, 'Usuario no encontrado' AS Mensaje;
        RETURN;
    END

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
