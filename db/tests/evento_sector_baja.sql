-- Escenario: deshabilitar un sector con entradas vendidas debe fallar con SIGNAL 45000.
--
-- Requisito previo: tener en la BD datos de prueba con:
--   - un evento con id_evento = 1 en el estadio "Centenario"
--   - los sectores A y B habilitados en ese evento
--   - una venta y entrada vendida en el sector A
-- Ajustar ids si el escenario de la DB de prueba usa otros.

-- ----------------------------------------------------------------
-- Caso 1 OK: deshabilitar sector B (sin entradas vendidas)
-- ----------------------------------------------------------------
DELETE FROM evento_sector WHERE id_evento = 1 AND nombre_estadio = 'Centenario' AND nombre_sector = 'B';
-- Esperado: 1 fila afectada, sin error.

-- Restaurar para el caso 2
INSERT IGNORE INTO evento_sector (id_evento, nombre_estadio, nombre_sector) VALUES (1, 'Centenario', 'B');

-- ----------------------------------------------------------------
-- Caso 2 FALLA: deshabilitar sector A que tiene una entrada vendida
-- ----------------------------------------------------------------
-- Verificar que hay al menos una entrada vendida en sector A del evento 1:
SELECT count(*) AS entradas_sector_a
FROM entrada
WHERE id_evento = 1 AND nombre_estadio = 'Centenario' AND nombre_sector = 'A';

-- Este DELETE debe lanzar SIGNAL 45000:
-- DELETE FROM evento_sector WHERE id_evento = 1 AND nombre_estadio = 'Centenario' AND nombre_sector = 'A';
-- => ERROR 1644 (45000): No se puede deshabilitar un sector que ya tiene entradas vendidas

-- Descomentar la línea de arriba para probar el rechazo.
