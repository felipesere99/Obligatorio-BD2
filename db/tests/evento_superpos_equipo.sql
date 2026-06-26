-- ============================================================
-- Test: tr_evento_superpos_ins / _upd — conflicto de equipo
-- en el mismo horario (cualquier estadio).
--
-- Ejecutar contra la base con seed cargado (o tras aplicar la
-- migración 20260625_evento_superpos_equipo.sql).
--
-- Escenario:
--   1. Evento existente (seed): URU vs ARG en Centenario, 2026-06-15 18:00–20:00.
--   2. Segundo estadio "Pocitos" (sin solape de estadio).
--   3. Segundo evento a la misma hora con URU como local → debe fallar.
--
-- Resultado esperado del paso 3:
--   ERROR 1644 (45000): Uno de los equipos ya juega otro partido en ese horario
-- ============================================================

START TRANSACTION;

-- Segundo estadio (evita el trigger de superposición por estadio)
INSERT INTO estadio(nombre, direccion) VALUES
    ('Pocitos', 'Av. Italia 4200, Montevideo');

-- Caso OK: otro horario, mismo equipo, otro estadio
INSERT INTO evento(nombre, fecha_inicio, fecha_fin, pais_local, pais_visitante, nombre_estadio) VALUES
    ('Uruguay vs Brasil (tarde)', '2026-06-15 14:00:00', '2026-06-15 16:00:00', 'URU', 'BRA', 'Pocitos');

-- Caso ERROR: mismo horario que el evento 1, URU comparte partido en otro estadio
-- Descomentar para verificar el rechazo:
/*
INSERT INTO evento(nombre, fecha_inicio, fecha_fin, pais_local, pais_visitante, nombre_estadio) VALUES
    ('Uruguay vs España (conflicto)', '2026-06-15 18:00:00', '2026-06-15 20:00:00', 'URU', 'ESP', 'Pocitos');
-- Esperado: SIGNAL 45000 — Uno de los equipos ya juega otro partido en ese horario
*/

-- Caso OK: mismo horario pero equipos distintos (no comparten URU/ARG del evento 1)
INSERT INTO evento(nombre, fecha_inicio, fecha_fin, pais_local, pais_visitante, nombre_estadio) VALUES
    ('Brasil vs España', '2026-06-15 18:00:00', '2026-06-15 20:00:00', 'BRA', 'ESP', 'Pocitos');

ROLLBACK;
