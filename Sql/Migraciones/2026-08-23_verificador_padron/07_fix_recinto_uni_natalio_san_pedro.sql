-- =========================================================================
-- Migración: 07_fix_recinto_uni_natalio_san_pedro.sql
-- Descripción: Corrección de asignación territorial del recinto UNIVERSIDAD NACIONAL DE ITAPUA
--              y reasignación de usuarios veedores de Natalio afectados.
--
-- Causa raíz:
-- En TB_Recinto existían dos recintos con el nombre 'UNIVERSIDAD NACIONAL DE ITAPUA' ambos
-- con IdMunicipio = 1244 (NATALIO):
--   1. 475037d3-ce8a-4de2-b375-45c5ccf4fdc0 -> Padrón real de NATALIO (Mesas 1 a 12, 4.170 votantes)
--   2. 6c05e4c6-2fed-48e6-983b-a4812cd2db95 -> Padrón real de SAN PEDRO DEL PARANA (Mesas 1 a 7, 2.351 votantes)
--
-- Al crear veedores para mesas 8, 9, etc. de Natalio, se les asignaba el GUID 6c05e4c6...,
-- el cual no contenía mesas superiores a la 7, resultando en 0 votantes mostrados en la App.
-- =========================================================================

-- 1. Reasignar el recinto de San Pedro del Paraná a su municipio correcto (1255)
UPDATE dbo.TB_Recinto
SET IdMunicipio = 1255
WHERE IdRecinto = '6c05e4c6-2fed-48e6-983b-a4812cd2db95';

-- 2. Reasignar a los usuarios de Natalio al recinto correcto de Natalio (475037d3...)
UPDATE dbo.Usuario
SET IdRecinto = '475037d3-ce8a-4de2-b375-45c5ccf4fdc0'
WHERE IdRecinto = '6c05e4c6-2fed-48e6-983b-a4812cd2db95'
  AND IdTerritorio = 1244;
