/*
    01. Agrega la columna IdUsuarioCreate a Usuario.

    Registra qué administrador creó cada usuario. Sin esta columna no hay
    forma de saber "quién creó a quién", que es la base de la restricción
    de admins territoriales (solo ven/gestionan lo que ellos crearon).

    Tipo INT (no BIGINT) para coincidir con IdUsuario/IdRol/IdTerritorio,
    que ya son INT en esta tabla.

    Idempotente: no falla si ya se ejecutó antes.
*/

IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Usuario')
      AND name = 'IdUsuarioCreate'
)
BEGIN
    ALTER TABLE dbo.Usuario ADD IdUsuarioCreate INT NULL;
END
GO
