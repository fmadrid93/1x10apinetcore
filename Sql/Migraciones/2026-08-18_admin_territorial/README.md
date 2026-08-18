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
