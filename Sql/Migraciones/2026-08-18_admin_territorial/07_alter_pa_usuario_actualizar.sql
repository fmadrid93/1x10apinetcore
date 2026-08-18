/*
    07. Actualiza pa_usuario_actualizar para grabar IdUsuarioUpdate,
    FechaUpdate y Motivo en cada edicion (§23.2 del manual).

    @IdUsuarioUpdate y @Motivo son opcionales (NULL por defecto) para no
    romper otros llamadores existentes que todavia no los manden.

    Se mantiene el contrato de retorno original (Exito/Mensaje) tal cual
    lo consume hoy el backend/frontend -- no se fuerza el @@ROWCOUNT del
    §23.5 aqui para no cambiar un contrato de API ya en uso.
*/

CREATE OR ALTER PROCEDURE pa_usuario_actualizar
    @IdUsuario INT,
    @IdRol INT,
    @IdTerritorio INT = NULL,
    @IdUsuarioSupervisor INT = NULL,
    @NombreCompleto VARCHAR(200),
    @CI VARCHAR(30) = NULL,
    @Celular VARCHAR(30) = NULL,
    @Email VARCHAR(150) = NULL,
    @Activo BIT = 1,
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
        IdRol = @IdRol,
        IdTerritorio = @IdTerritorio,
        IdUsuarioSupervisor = @IdUsuarioSupervisor,
        NombreCompleto = @NombreCompleto,
        CI = @CI,
        Celular = @Celular,
        Email = @Email,
        Activo = @Activo,
        FechaUpdate = GETDATE(),
        IdUsuarioUpdate = @IdUsuarioUpdate,
        Motivo = NULLIF(@Motivo, '')
    WHERE IdUsuario = @IdUsuario;

    SELECT 1 AS Exito, 'Usuario actualizado correctamente' AS Mensaje;
END
GO
