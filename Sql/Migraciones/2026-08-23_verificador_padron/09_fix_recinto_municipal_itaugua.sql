-- =========================================================================
-- Migración: 09_fix_recinto_municipal_itaugua.sql
-- Descripción: Vinculación del padrón electoral de Itauguá (Recinto Municipalidad)
--              al GUID '7984352a-c208-4536-b06d-1a989ae495a1' (RECINTO MUNICIPAL - ITAUGUA)
--              para habilitar a los verificadores de Itauguá (muni1 a muni15 y edumeza).
-- =========================================================================

UPDATE dbo.TB_Votante
SET IdRecintoOk = '7984352a-c208-4536-b06d-1a989ae495a1',
    RecintoVotacion = 'RECINTO MUNICIPAL - ITAUGUA'
WHERE Distrito = 'ITAUGUA' AND IdRecintoOk = '364c3fea-17b0-47d1-b0f2-cae752d436da';
GO
