/*
    11. Backfill de IdUsuarioCreate para gerentes sembrados directamente en
    la base (sin pasar por la API), donde SÍ se puede inferir el creador.

    Regla de negocio confirmada: cuando un admin registra un gerente, ese
    gerente queda con el admin como IdUsuarioSupervisor. Por lo tanto, para
    un gerente sin IdUsuarioCreate pero con IdUsuarioSupervisor poblado,
    el supervisor ES el creador real.

    Alcance: SOLO gerentes (IdRol = 2). No aplica a movilizadores, donde
    IdUsuarioSupervisor (el gerente que lo supervisa) no siempre coincide
    con IdUsuarioCreate (quien lo registró pudo ser un admin distinto).

    Antes de ejecutar, revisar cuántas filas afectaría:

        SELECT COUNT(*)
        FROM Usuario
        WHERE IdRol = 2
          AND IdUsuarioCreate IS NULL
          AND IdUsuarioSupervisor IS NOT NULL;

    Idempotente: solo toca filas con IdUsuarioCreate IS NULL, así que
    volver a correrlo no cambia nada si ya se ejecutó.
*/

UPDATE Usuario
SET IdUsuarioCreate = IdUsuarioSupervisor
WHERE IdRol = 2
  AND IdUsuarioCreate IS NULL
  AND IdUsuarioSupervisor IS NOT NULL;
GO
