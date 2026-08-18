/*
    02. Backfill de IdUsuarioCreate para usuarios históricos.

    Todos los usuarios creados ANTES de este cambio no tienen creador
    registrado (IdUsuarioCreate quedó NULL al agregar la columna en el
    script 01). Se les asigna como creador a la cuenta "superadmin"
    (super admin, sin territorio), que ya existe en la base con
    IdUsuario = 3007.

    Con esto ningún registro queda con IdUsuarioCreate NULL, y esos
    usuarios históricos solo son visibles para super admins (un admin
    territorial nunca los creó, así que no le pertenecen).

    IMPORTANTE: verificar antes de correr que el ID de "superadmin" sigue
    siendo 3007 en este ambiente:

        SELECT IdUsuario, Usuario, IdTerritorio FROM Usuario WHERE Usuario = 'superadmin';

    Si es distinto, ajustar la constante @IdSuperAdmin abajo.

    Idempotente: solo toca filas con IdUsuarioCreate IS NULL, así que
    volver a correrlo no cambia nada si ya se ejecutó.
*/

DECLARE @IdSuperAdmin INT = 3007;

UPDATE dbo.Usuario
SET IdUsuarioCreate = @IdSuperAdmin
WHERE IdUsuarioCreate IS NULL
  AND IdUsuario <> @IdSuperAdmin;
GO
