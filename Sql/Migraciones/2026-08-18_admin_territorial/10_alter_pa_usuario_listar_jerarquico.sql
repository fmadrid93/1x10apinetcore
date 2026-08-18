/*
    10. Extiende pa_usuario_listar para soportar visibilidad jerarquica
    (item 3: "el gerente tambien puede registrar usuarios").

    Reglas confirmadas con el cliente:
      - Admin territorial: ve lo que EL creo + lo que crearon SUS gerentes
        (jerarquico, no solo creacion directa).
      - Gerente: ve los movilizadores/verificadores que EL creo, MAS los
        movilizadores que le asignaron como supervisor (aunque los haya
        creado otro, p.ej. el admin). Los verificadores NO tienen concepto
        de supervisor: solo los ve/gestiona quien los creo.
      - Super admin: sin filtro.

    Se reemplaza el filtro simple "@IdUsuarioCreate = X" por dos parametros
    combinables:

      @IdsCreador       VARCHAR(MAX) -- CSV de IdUsuario: incluye filas
                                          cuyo IdUsuarioCreate este en la lista.
      @IdSupervisorPropio INT        -- ademas incluye filas cuyo
                                          IdUsuarioSupervisor sea este id.

    Si ambos son NULL, no se filtra (super admin). El backend arma
    @IdsCreador segun el rol de quien llama:
      - Admin: "idAdmin,idGerente1,idGerente2,..."
      - Gerente: "idGerente"
    y @IdSupervisorPropio = idGerente solo cuando quien llama es gerente
    (para admin va NULL, porque su alcance ya es jerarquico via @IdsCreador).
*/

CREATE OR ALTER PROCEDURE pa_usuario_listar
    @IdRol INT = NULL,
    @IdTerritorio INT = NULL,
    @IdUsuarioSupervisor INT = NULL,
    @SoloActivos BIT = 1,
    @IdUsuarioCreate INT = NULL,
    @IdsCreador VARCHAR(MAX) = NULL,
    @IdSupervisorPropio INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.IdUsuario,
        u.IdRol,
        r.Nombre AS Rol,
        u.IdTerritorio,
        t.Nombre AS Territorio,
        u.IdUsuarioSupervisor,
        s.NombreCompleto AS Supervisor,
        u.Usuario,
        u.NombreCompleto,
        u.CI,
        u.Celular,
        u.Email,
        u.Activo,
        u.FechaCreate,
        u.FechaUpdate
    FROM Usuario u
    INNER JOIN Rol r ON r.IdRol = u.IdRol
    LEFT JOIN Territorio t ON t.IdTerritorio = u.IdTerritorio
    LEFT JOIN Usuario s ON s.IdUsuario = u.IdUsuarioSupervisor
    WHERE (@IdRol IS NULL OR u.IdRol = @IdRol)
      AND (@IdTerritorio IS NULL OR u.IdTerritorio = @IdTerritorio)
      AND (@IdUsuarioSupervisor IS NULL OR u.IdUsuarioSupervisor = @IdUsuarioSupervisor)
      AND (@SoloActivos = 0 OR u.Activo = 1)
      AND (@IdUsuarioCreate IS NULL OR u.IdUsuarioCreate = @IdUsuarioCreate)
      AND (
            (@IdsCreador IS NULL AND @IdSupervisorPropio IS NULL)
            OR (@IdsCreador IS NOT NULL AND u.IdUsuarioCreate IN (SELECT CAST(value AS INT) FROM STRING_SPLIT(@IdsCreador, ',')))
            OR (@IdSupervisorPropio IS NOT NULL AND u.IdUsuarioSupervisor = @IdSupervisorPropio)
          )
    ORDER BY r.Nombre, u.NombreCompleto;
END
GO
