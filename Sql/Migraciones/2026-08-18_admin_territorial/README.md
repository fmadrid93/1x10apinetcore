# Migración: admins territoriales solo ven/gestionan lo que crearon

Ejecutar los scripts **en orden** contra la base `AppCampana1x10`.

1. `01_alter_usuario_add_idusuariocreate.sql` — agrega la columna
   `IdUsuarioCreate` a `Usuario`.
2. `02_backfill_idusuariocreate_historicos.sql` — asigna como creador a la
   cuenta `superadmin` (`IdUsuario = 3007`) a todos los usuarios que ya
   existían antes de este cambio (no tienen creador registrado).
3. `03_alter_pa_usuario_insertar.sql` — actualiza `pa_usuario_insertar` para
   que reciba y grabe `@IdUsuarioCreate`.
4. `04_alter_pa_usuario_listar.sql` — actualiza `pa_usuario_listar` para que
   pueda filtrar por `@IdUsuarioCreate`.
5. `05_alter_pa_usuario_obtener_por_id.sql` — agrega `IdUsuarioCreate` al
   `SELECT` de `pa_usuario_obtener_por_id`, necesario para que el backend
   valide ownership antes de editar/cambiar clave/dar de baja.
6. `06_alter_usuario_add_control_fields.sql` — completa los campos de
   control que faltaban en `Usuario` según el manual de estándares (§5.3/§18):
   `IdUsuarioUpdate`, `IdUsuarioDelete`, `FechaDelete`, `Motivo`.
7. `07_alter_pa_usuario_actualizar.sql` — graba `IdUsuarioUpdate`,
   `FechaUpdate` y `Motivo` en cada edición (§23.2).
8. `08_alter_pa_usuario_cambiar_clave.sql` — ídem para el cambio de clave.
9. `09_alter_pa_usuario_eliminar_logico.sql` — graba `IdUsuarioDelete`,
   `FechaDelete` y `Motivo` en la baja lógica (§23.3); antes reusaba
   `FechaUpdate` por error, ahora usa sus propios campos.
10. `10_alter_pa_usuario_listar_jerarquico.sql` — ítem 3: el gerente también
    puede registrar usuarios. Extiende `pa_usuario_listar` con `@IdsCreador`
    (CSV de ids, visibilidad jerárquica: admin ve lo suyo + lo de sus
    gerentes) y `@IdSupervisorPropio` (un gerente también ve los
    movilizadores que le asignaron como supervisor, aunque no los haya
    creado él).
11. `11_backfill_gerentes_creador_inferido.sql` — para usuarios cargados
    directo en la base (sin pasar por la API) que quedaron con
    `IdUsuarioCreate` vacío: en gerentes, el `IdUsuarioSupervisor` ya
    apunta al admin real que los "creó" (regla de negocio), así que se usa
    ese valor como creador. **Correr antes que el script 12.**
12. `12_backfill_admins_sin_rastro_a_superadmin.sql` — para usuarios sin
    `IdUsuarioCreate` **ni** `IdUsuarioSupervisor` (típicamente admins
    sembrados directo, ya que un admin nunca tiene supervisor) — no hay
    forma de inferir su creador real, así que se les asigna `superadmin`
    como creador, siguiendo la misma convención del script 02.

Todos los scripts usan `CREATE OR ALTER` / verificación previa, así que se
pueden volver a correr sin error si ya se ejecutaron antes.

Antes de correr el paso 2, confirmar que `superadmin` sigue teniendo
`IdUsuario = 3007`:

```sql
SELECT IdUsuario, Usuario, IdTerritorio FROM Usuario WHERE Usuario = 'superadmin';
```

Si el ID fuera distinto (por ejemplo en el ambiente de producción), ajustar
el valor `3007` en `02_backfill_idusuariocreate_historicos.sql` antes de
ejecutarlo.
