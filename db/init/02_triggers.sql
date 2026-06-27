-- ============================================================
--  02_triggers.sql — Reglas de negocio que no se pueden expresar
--  con constraints declarativos (referenciadas desde 01_schema.sql).
--  Dialecto: MySQL 8. Las RAISE EXCEPTION de PostgreSQL se traducen a
--  SIGNAL SQLSTATE '45000' (el middleware las mapea a HTTP 400).
--  MySQL no permite "BEFORE INSERT OR UPDATE": se crea un trigger por evento.
-- ============================================================

DROP TRIGGER IF EXISTS tr_evento_superpos_ins;
DROP TRIGGER IF EXISTS tr_evento_superpos_upd;
DROP TRIGGER IF EXISTS tr_evento_sector_ins;
DROP TRIGGER IF EXISTS tr_evento_sector_upd;
DROP TRIGGER IF EXISTS tr_evento_sector_del;
DROP TRIGGER IF EXISTS tr_entrada_sector_habilitado;
DROP TRIGGER IF EXISTS tr_max_entradas_por_venta;
DROP TRIGGER IF EXISTS tr_capacidad_sector;
DROP TRIGGER IF EXISTS tr_validacion_irreversible;
DROP TRIGGER IF EXISTS tr_validacion_dispositivo;
DROP TRIGGER IF EXISTS tr_transferencia_control;
DROP TRIGGER IF EXISTS tr_entrada_tenencia_inicial;
DROP TRIGGER IF EXISTS tr_transferencia_aceptada;

DELIMITER //

-- ------------------------------------------------------------
-- 1) No superposición de eventos en un mismo estadio.
-- ------------------------------------------------------------
CREATE TRIGGER tr_evento_superpos_ins
    BEFORE INSERT ON evento FOR EACH ROW
BEGIN
    IF EXISTS (
        SELECT 1 FROM evento e
        WHERE e.nombre_estadio = NEW.nombre_estadio
          AND NEW.fecha_inicio < e.fecha_fin
          AND NEW.fecha_fin    > e.fecha_inicio
    ) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Superposición de eventos en el estadio en ese rango de fechas';
    END IF;

    IF EXISTS (
        SELECT 1 FROM evento e
        WHERE NEW.fecha_inicio < e.fecha_fin
          AND NEW.fecha_fin    > e.fecha_inicio
          AND (
              (NEW.pais_local IS NOT NULL
               AND (e.pais_local = NEW.pais_local OR e.pais_visitante = NEW.pais_local))
              OR
              (NEW.pais_visitante IS NOT NULL
               AND (e.pais_local = NEW.pais_visitante OR e.pais_visitante = NEW.pais_visitante))
          )
    ) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Uno de los equipos ya juega otro partido en ese horario';
    END IF;
END //

CREATE TRIGGER tr_evento_superpos_upd
    BEFORE UPDATE ON evento FOR EACH ROW
BEGIN
    IF EXISTS (
        SELECT 1 FROM evento e
        WHERE e.nombre_estadio = NEW.nombre_estadio
          AND e.id_evento     <> NEW.id_evento
          AND NEW.fecha_inicio < e.fecha_fin
          AND NEW.fecha_fin    > e.fecha_inicio
    ) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Superposición de eventos en el estadio en ese rango de fechas';
    END IF;

    IF EXISTS (
        SELECT 1 FROM evento e
        WHERE e.id_evento     <> NEW.id_evento
          AND NEW.fecha_inicio < e.fecha_fin
          AND NEW.fecha_fin    > e.fecha_inicio
          AND (
              (NEW.pais_local IS NOT NULL
               AND (e.pais_local = NEW.pais_local OR e.pais_visitante = NEW.pais_local))
              OR
              (NEW.pais_visitante IS NOT NULL
               AND (e.pais_local = NEW.pais_visitante OR e.pais_visitante = NEW.pais_visitante))
          )
    ) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Uno de los equipos ya juega otro partido en ese horario';
    END IF;
END //

-- ------------------------------------------------------------
-- 2) Un sector habilitado para un evento debe pertenecer al
--    estadio donde se juega ese evento.
-- ------------------------------------------------------------
CREATE TRIGGER tr_evento_sector_ins
    BEFORE INSERT ON evento_sector FOR EACH ROW
BEGIN
    DECLARE v_estadio_evento VARCHAR(120);
    SELECT nombre_estadio INTO v_estadio_evento FROM evento WHERE id_evento = NEW.id_evento;
    IF v_estadio_evento <> NEW.nombre_estadio THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'El sector no pertenece al estadio del evento';
    END IF;
END //

CREATE TRIGGER tr_evento_sector_upd
    BEFORE UPDATE ON evento_sector FOR EACH ROW
BEGIN
    DECLARE v_estadio_evento VARCHAR(120);
    SELECT nombre_estadio INTO v_estadio_evento FROM evento WHERE id_evento = NEW.id_evento;
    IF v_estadio_evento <> NEW.nombre_estadio THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'El sector no pertenece al estadio del evento';
    END IF;
END //

-- ------------------------------------------------------------
-- 5) No se puede deshabilitar un sector que ya tiene entradas
--    vendidas para ese evento.
-- ------------------------------------------------------------
CREATE TRIGGER tr_evento_sector_del
    BEFORE DELETE ON evento_sector FOR EACH ROW
BEGIN
    IF EXISTS (
        SELECT 1 FROM entrada
        WHERE id_evento      = OLD.id_evento
          AND nombre_estadio = OLD.nombre_estadio
          AND nombre_sector  = OLD.nombre_sector
    ) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'No se puede deshabilitar un sector que ya tiene entradas vendidas';
    END IF;
END //

-- ------------------------------------------------------------
-- 3) Una entrada solo puede emitirse para un sector HABILITADO
--    para ese evento (garantiza también estadio coherente).
-- ------------------------------------------------------------
CREATE TRIGGER tr_entrada_sector_habilitado
    BEFORE INSERT ON entrada FOR EACH ROW
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM evento_sector es
        WHERE es.id_evento      = NEW.id_evento
          AND es.nombre_estadio = NEW.nombre_estadio
          AND es.nombre_sector  = NEW.nombre_sector
    ) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'El sector no está habilitado para el evento';
    END IF;
END //

-- ------------------------------------------------------------
-- 4) Máximo 5 entradas por venta.
-- ------------------------------------------------------------
CREATE TRIGGER tr_max_entradas_por_venta
    BEFORE INSERT ON entrada FOR EACH ROW
BEGIN
    DECLARE v_cant INT;
    SELECT count(*) INTO v_cant FROM entrada WHERE nro_venta = NEW.nro_venta;
    IF v_cant >= 5 THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'La venta ya tiene 5 entradas (máximo permitido)';
    END IF;
END //

-- ------------------------------------------------------------
-- 5) Capacidad por sector (límite duro) para un mismo evento.
-- ------------------------------------------------------------
CREATE TRIGGER tr_capacidad_sector
    BEFORE INSERT ON entrada FOR EACH ROW
BEGIN
    DECLARE v_capacidad INT;
    DECLARE v_vendidas  INT;

    SELECT capacidad INTO v_capacidad
    FROM sector
    WHERE nombre_estadio = NEW.nombre_estadio AND nombre = NEW.nombre_sector;

    SELECT count(*) INTO v_vendidas
    FROM entrada
    WHERE id_evento      = NEW.id_evento
      AND nombre_estadio = NEW.nombre_estadio
      AND nombre_sector  = NEW.nombre_sector;

    IF v_vendidas >= v_capacidad THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Capacidad agotada en el sector para el evento';
    END IF;
END //

-- ------------------------------------------------------------
-- 6) Validación irreversible: una vez registrada la hora_validacion
--    no puede volver a NULL.
-- ------------------------------------------------------------
CREATE TRIGGER tr_validacion_irreversible
    BEFORE UPDATE ON entrada FOR EACH ROW
BEGIN
    IF OLD.hora_validacion IS NOT NULL AND NEW.hora_validacion IS NULL THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'La validación de la entrada es irreversible';
    END IF;
END //

-- ------------------------------------------------------------
-- 7) Transferencia: máx. 3 por entrada, no transferir una entrada
--    ya validada, y numerar el contador en la cadena de custodia.
-- ------------------------------------------------------------
CREATE TRIGGER tr_validacion_dispositivo
    BEFORE INSERT ON validacion FOR EACH ROW
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM dispositivo d
        JOIN funcionario_dispositivo fd ON fd.id_dispositivo = d.id_dispositivo
        WHERE d.id_dispositivo = NEW.id_dispositivo
          AND d.habilitado = TRUE
          AND fd.doc_funcionario = NEW.doc_funcionario
    ) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'El dispositivo no esta habilitado o no esta asignado al funcionario';
    END IF;
END //

CREATE TRIGGER tr_transferencia_control
    BEFORE INSERT ON transferencia FOR EACH ROW
BEGIN
    DECLARE v_validada   DATETIME(6);
    DECLARE v_aceptadas  INT;

    SELECT hora_validacion INTO v_validada FROM entrada WHERE nro_entrada = NEW.nro_entrada;
    IF v_validada IS NOT NULL THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'La entrada ya fue validada: no admite transferencias';
    END IF;

    SELECT count(*) INTO v_aceptadas
    FROM transferencia
    WHERE nro_entrada = NEW.nro_entrada AND estado = 'aceptada';
    IF v_aceptadas >= 3 THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'La entrada ya alcanzó el máximo de 3 transferencias';
    END IF;

    -- contador refleja el número de orden entre las aceptadas, nunca supera 3
    SET NEW.contador = v_aceptadas + 1;
END //

-- ------------------------------------------------------------
-- 8) Tenencia inicial: al emitir una entrada, su propietario la
--    "tiene" (mantiene usuario_tiene_entradas poblada).
-- ------------------------------------------------------------
CREATE TRIGGER tr_entrada_tenencia_inicial
    AFTER INSERT ON entrada FOR EACH ROW
BEGIN
    INSERT INTO usuario_tiene_entradas (documento_usuario, nro_entrada)
    VALUES (NEW.doc_propietario, NEW.nro_entrada);
END //

-- ------------------------------------------------------------
-- 9) Aceptación de transferencia: cambia el propietario de la
--    entrada y actualiza la tenencia (emisor -> receptor).
-- ------------------------------------------------------------
CREATE TRIGGER tr_transferencia_aceptada
    AFTER UPDATE ON transferencia FOR EACH ROW
BEGIN
    IF NEW.estado = 'aceptada' AND OLD.estado <> 'aceptada' THEN
        UPDATE entrada
        SET doc_propietario = NEW.doc_receptor
        WHERE nro_entrada = NEW.nro_entrada;

        DELETE FROM usuario_tiene_entradas
        WHERE nro_entrada = NEW.nro_entrada AND documento_usuario = NEW.doc_emisor;

        INSERT IGNORE INTO usuario_tiene_entradas (documento_usuario, nro_entrada)
        VALUES (NEW.doc_receptor, NEW.nro_entrada);
    END IF;
END //

DELIMITER ;
