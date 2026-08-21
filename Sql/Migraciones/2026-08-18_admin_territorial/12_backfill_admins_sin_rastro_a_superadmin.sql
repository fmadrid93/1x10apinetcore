/*
    12. Backfill de IdUsuarioCreate para usuarios sembrados directamente en
    la base donde NO se puede inferir el creador (no tienen ni
    IdUsuarioCreate ni IdUsuarioSupervisor -- típicamente admins, ya que el
    campo "supervisor" solo se usa para gerentes y movilizadores, nunca
    para admins).

    Sin ningún dato en la fila que indique quién los creó, se aplica la
    misma convención ya usada en el backfill original de históricos
    (script 02): se les asigna como creador la cuenta "superadmin".

    IMPORTANTE: verificar antes de correr que el ID de "superadmin" sigue
    siendo 3007 en este ambiente:

        SELECT IdUsuario, Usuario, IdTerritorio FROM Usuario WHERE Usuario = 'superadmin';

    Si es distinto, ajustar la constante @IdSuperAdmin abajo.

    Revisar antes cuántas filas afectaría:

        SELECT COUNT(*)
        FROM Usuario
        WHERE IdUsuarioCreate IS NULL
          AND IdUsuarioSupervisor IS NULL
          AND IdUsuario <> 3007;

    Idempotente: solo toca filas con IdUsuarioCreate IS NULL, así que
    volver a correrlo no cambia nada si ya se ejecutó. Se recomienda correr
    este script DESPUÉS del 11 (para no "atrapar" gerentes inferibles con
    el fallback genérico de superadmin).
*/

DECLARE @IdSuperAdmin INT = 3007;

UPDATE Usuario
SET IdUsuarioCreate = @IdSuperAdmin
WHERE IdUsuarioCreate IS NULL
  AND IdUsuarioSupervisor IS NULL
  AND IdUsuario <> @IdSuperAdmin;
GO
