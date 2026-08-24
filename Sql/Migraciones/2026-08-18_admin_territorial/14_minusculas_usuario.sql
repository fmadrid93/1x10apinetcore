/*
    14. Pasa a minusculas el campo Usuario (login) de TODOS los usuarios.

    Usuario tiene un indice unico FILTRADO (UX_Usuario_Usuario), asi que
    igual que en los procedures, cualquier UPDATE contra esta tabla exige
    QUOTED_IDENTIFIER ON en la sesion (ver el problema que ya tuvimos con
    pa_usuario_cambiar_clave).

    OJO: si la collation de la columna es case-sensitive y existen dos
    usuarios que solo difieren en mayusculas/minusculas (ej. "Miguel" y
    "miguel"), este UPDATE fallaria por violar el indice unico. El primer
    SELECT de abajo detecta ese caso antes de tocar nada -- si devuelve
    filas, hay que resolver esos duplicados a mano antes de correr el UPDATE.
*/

SET QUOTED_IDENTIFIER ON;

-- 1) Chequeo previo: usuarios que colisionarian al pasar a minusculas
SELECT LOWER(Usuario) AS UsuarioEnMinuscula, COUNT(*) AS Cantidad
FROM Usuario
GROUP BY LOWER(Usuario)
HAVING COUNT(*) > 1;

-- 2) Si el SELECT de arriba no devuelve filas, recien ahi correr esto:
UPDATE Usuario
SET Usuario = LOWER(Usuario)
WHERE Usuario <> LOWER(Usuario);
