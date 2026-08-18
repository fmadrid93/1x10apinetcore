/*
    03. Actualiza pa_usuario_insertar para recibir y grabar @IdUsuarioCreate.

    @IdUsuarioCreate es el IdUsuario del admin que está creando el registro
    (lo saca el backend del JWT, nunca lo manda el frontend directamente).
    Se hace opcional (= NULL) para no romper otros llamadores existentes del
    procedure que todavía no manden el parámetro.

    Se preserva tal cual el resto de la lógica original (validación de
    usuario duplicado, inserción, creación automática de MovilizadorMeta
    para el rol MOVILIZADOR).
*/

CREATE OR ALTER PROCEDURE pa_usuario_insertar
    @IdRol INT,
    @IdTerritorio INT = NULL,
    @IdUsuarioSupervisor INT = NULL,
    @Usuario VARCHAR(50),
    @ClaveHash VARCHAR(300),
    @NombreCompleto VARCHAR(200),
    @CI VARCHAR(30) = NULL,
    @Celular VARCHAR(30) = NULL,
    @Email VARCHAR(150) = NULL,
    @IdUsuarioCreate INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM Usuario WHERE Usuario = @Usuario)
    BEGIN
        SELECT 0 AS Exito, 'El nombre de usuario ya existe' AS Mensaje;
        RETURN;
    END

    INSERT INTO Usuario
    (
        IdRol,
        IdTerritorio,
        IdUsuarioSupervisor,
        Usuario,
        ClaveHash,
        NombreCompleto,
        CI,
        Celular,
        Email,
        Activo,
        FechaCreate,
        IdUsuarioCreate
    )
    VALUES
    (
        @IdRol,
        @IdTerritorio,
        @IdUsuarioSupervisor,
        @Usuario,
        @ClaveHash,
        @NombreCompleto,
        @CI,
        @Celular,
        @Email,
        1,
        GETDATE(),
        @IdUsuarioCreate
    );

    DECLARE @IdUsuario INT = SCOPE_IDENTITY();

    IF EXISTS (
        SELECT 1
        FROM Rol
        WHERE IdRol = @IdRol
          AND Nombre = 'MOVILIZADOR'
    )
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM MovilizadorMeta WHERE IdUsuarioMovilizador = @IdUsuario)
        BEGIN
            INSERT INTO MovilizadorMeta (IdUsuarioMovilizador, MetaObjetivo, FechaInicio, Activo)
            VALUES (@IdUsuario, 10, GETDATE(), 1);
        END
    END

    SELECT 1 AS Exito, @IdUsuario AS IdUsuario, 'Usuario registrado correctamente' AS Mensaje;
END
GO
