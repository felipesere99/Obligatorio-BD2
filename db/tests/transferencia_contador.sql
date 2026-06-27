-- Escenario: crear+cancelar transferencias varias veces y luego completar 3 aceptadas.
-- Sin el fix, el CHECK (contador BETWEEN 1 AND 3) falla al 4ª insert total aunque solo
-- haya 2 aceptadas. Con el fix, contador = aceptadas + 1, siempre ∈ {1,2,3}.
--
-- Requisito previo: tener en la BD al menos:
--   - una entrada con nro_entrada = 9999 (no validada)
--   - usuarios doc_emisor = '00000001' y doc_receptor = '00000002'
-- Ajustar los valores si el escenario de la DB de prueba usa otros.

-- Limpiar estado previo del escenario
DELETE FROM transferencia WHERE nro_entrada = 9999;

-- ----------------------------------------------------------------
-- Ronda 1: crear y cancelar (simula inicio → cancelación)
-- ----------------------------------------------------------------
INSERT INTO transferencia (nro_entrada, doc_emisor, doc_receptor, estado, fecha)
VALUES (9999, '00000001', '00000002', 'pendiente', NOW());
-- cancelar: estado pasa a 'cancelada' (no 'aceptada')
UPDATE transferencia
SET estado = 'cancelada'
WHERE nro_entrada = 9999 ORDER BY contador DESC LIMIT 1;

-- ----------------------------------------------------------------
-- Ronda 2: otra vuelta pendiente → cancelada
-- ----------------------------------------------------------------
INSERT INTO transferencia (nro_entrada, doc_emisor, doc_receptor, estado, fecha)
VALUES (9999, '00000001', '00000002', 'pendiente', NOW());
UPDATE transferencia
SET estado = 'cancelada'
WHERE nro_entrada = 9999 AND estado = 'pendiente' ORDER BY contador DESC LIMIT 1;

-- ----------------------------------------------------------------
-- Ronda 3 (y 4): ahora las 3 aceptadas reales
-- ----------------------------------------------------------------
INSERT INTO transferencia (nro_entrada, doc_emisor, doc_receptor, estado, fecha)
VALUES (9999, '00000001', '00000002', 'pendiente', NOW());
UPDATE transferencia SET estado = 'aceptada'
WHERE nro_entrada = 9999 AND estado = 'pendiente' ORDER BY contador DESC LIMIT 1;

INSERT INTO transferencia (nro_entrada, doc_emisor, doc_receptor, estado, fecha)
VALUES (9999, '00000002', '00000001', 'pendiente', NOW());
UPDATE transferencia SET estado = 'aceptada'
WHERE nro_entrada = 9999 AND estado = 'pendiente' ORDER BY contador DESC LIMIT 1;

INSERT INTO transferencia (nro_entrada, doc_emisor, doc_receptor, estado, fecha)
VALUES (9999, '00000001', '00000002', 'pendiente', NOW());
UPDATE transferencia SET estado = 'aceptada'
WHERE nro_entrada = 9999 AND estado = 'pendiente' ORDER BY contador DESC LIMIT 1;

-- ----------------------------------------------------------------
-- Verificación: 3 aceptadas, 2 canceladas, contadores 1..3 sin gaps
-- ----------------------------------------------------------------
SELECT estado, contador FROM transferencia WHERE nro_entrada = 9999 ORDER BY contador;
-- Esperado: dos filas canceladas (contador 1,1) y tres aceptadas con contador 1,2,3

-- ----------------------------------------------------------------
-- 4ª intento de aceptada debe fallar con SIGNAL 45000
-- ----------------------------------------------------------------
-- Descomentar para probar el rechazo:
-- INSERT INTO transferencia (nro_entrada, doc_emisor, doc_receptor, estado, fecha)
-- VALUES (9999, '00000002', '00000001', 'pendiente', NOW());
-- UPDATE transferencia SET estado = 'aceptada'
-- WHERE nro_entrada = 9999 AND estado = 'pendiente' ORDER BY contador DESC LIMIT 1;
-- => ERROR: 'La entrada ya alcanzó el máximo de 3 transferencias'

-- Limpieza
DELETE FROM transferencia WHERE nro_entrada = 9999;
