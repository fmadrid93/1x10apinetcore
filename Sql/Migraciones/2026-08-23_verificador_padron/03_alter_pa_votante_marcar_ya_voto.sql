-- =========================================================================
-- Procedimiento: PA_VOTANTE_MARCAR_YA_VOTO
-- Descripción: Actualiza el parámetro @IdVotante a VARCHAR(150) / UNIQUEIDENTIFIER
--              para prevenir incompatibilidad de tipos (Operand type clash).
-- =========================================================================

CREATE OR ALTER PROCEDURE dbo.PA_VOTANTE_MARCAR_YA_VOTO
    @IdVotante VARCHAR(150),
    @IdUsuarioMarca INT,
    @Observacion VARCHAR(300) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Votante
    SET EstadoDiaD = 'YA_VOTO',
        FechaMarcaDiaD = GETDATE()
    WHERE LTRIM(RTRIM(CAST(IdVotante AS VARCHAR(150)))) = LTRIM(RTRIM(@IdVotante));

    SELECT @@ROWCOUNT AS FilasAfectadas;
END;
GO
