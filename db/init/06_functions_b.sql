-- ============================================================
--  06_functions_b.sql — Procedimientos de Persona B
--  (Validación de ingreso y QR dinámico). Dialecto: MySQL 8.
--
--  Se aplica en caliente sin perder datos:
--    docker compose exec -T db \
--      mysql -uticketing -pticketing ticketing < db/init/06_functions_b.sql
-- ============================================================

DROP PROCEDURE IF EXISTS sp_generar_qr;
DROP PROCEDURE IF EXISTS sp_validar_entrada;

DELIMITER //

-- ------------------------------------------------------------
--  B1 — QR dinámico (generar / rotar)
-- ------------------------------------------------------------
-- sp_generar_qr — genera (o rota) el código QR activo de una entrada.
--   * Desactiva el código activo anterior (si existe) y crea uno nuevo.
--   * El token es único (codigo_qr.uq_codigo) gracias al UUID.
--   * Solo un código activo por entrada lo garantiza la columna generada
--     uq_codigo_activo_entrada del esquema.
-- Pensado para que el cliente lo invoque cada ~30s y muestre siempre un
-- código fresco. Devuelve el código recién creado.
CREATE PROCEDURE sp_generar_qr(IN p_nro_entrada INT)
BEGIN
    DECLARE v_validada DATETIME(6);
    DECLARE v_existe   INT;
    DECLARE v_codigo   VARCHAR(255);
    DECLARE v_id       INT;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    IF p_nro_entrada IS NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La entrada es obligatoria.';
    END IF;

    SELECT count(*), MAX(hora_validacion)
    INTO v_existe, v_validada
    FROM entrada
    WHERE nro_entrada = p_nro_entrada;

    IF v_existe = 0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'No existe la entrada.';
    END IF;
    IF v_validada IS NOT NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La entrada ya fue validada: no se puede generar un QR.';
    END IF;

    START TRANSACTION;

    -- Rotación: el código activo anterior queda inactivo.
    UPDATE codigo_qr
    SET activo = FALSE
    WHERE nro_entrada = p_nro_entrada AND activo = TRUE;

    -- Token único e impredecible para el nuevo código activo.
    SET v_codigo = CONCAT('QR-', p_nro_entrada, '-', REPLACE(UUID(), '-', ''));

    INSERT INTO codigo_qr(nro_entrada, codigo, activo)
    VALUES (p_nro_entrada, v_codigo, TRUE);
    SET v_id = LAST_INSERT_ID();

    COMMIT;

    SELECT c.id_codigo, c.nro_entrada, c.codigo, c.generado_en
    FROM codigo_qr c
    WHERE c.id_codigo = v_id;
END //

-- ------------------------------------------------------------
--  B2 — Validación de ingreso (consume la entrada)
-- ------------------------------------------------------------
-- sp_validar_entrada — valida el ingreso de una entrada a partir del código
-- QR escaneado por un funcionario con su dispositivo.
--   1) El QR debe estar activo (codigo_qr.activo) -> si no, QR inválido/expirado.
--   2) La entrada no puede estar ya validada (hora_validacion) -> ingreso único.
--   3) El dispositivo debe pertenecer al funcionario (funcionario_dispositivo).
--   4) El funcionario debe estar asignado al sector del evento de la entrada
--      (funcionario_asignado).
-- Si todo es válido: registra la validación (funcionario + código + dispositivo),
-- marca la entrada como validada (hora_validacion) — operación irreversible por
-- el trigger tr_validacion_irreversible — y consume (desactiva) el código QR.
-- Es atómica: el EXIT HANDLER revierte todo y re-lanza el error.
CREATE PROCEDURE sp_validar_entrada(
    IN p_codigo         VARCHAR(255),
    IN p_doc_funcionario VARCHAR(30),
    IN p_id_dispositivo INT
)
BEGIN
    DECLARE v_id_codigo   INT;
    DECLARE v_nro_entrada INT;
    DECLARE v_validada    DATETIME(6);
    DECLARE v_id_evento   INT;
    DECLARE v_estadio     VARCHAR(120);
    DECLARE v_sector      VARCHAR(80);

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    IF p_codigo IS NULL OR TRIM(p_codigo) = '' THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El código QR es obligatorio.';
    END IF;
    IF p_doc_funcionario IS NULL OR TRIM(p_doc_funcionario) = '' THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El funcionario es obligatorio.';
    END IF;
    IF p_id_dispositivo IS NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El dispositivo es obligatorio.';
    END IF;

    -- 1) QR activo.
    SELECT id_codigo, nro_entrada
    INTO v_id_codigo, v_nro_entrada
    FROM codigo_qr
    WHERE codigo = p_codigo AND activo = TRUE
    LIMIT 1;

    IF v_id_codigo IS NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Código QR inválido o expirado.';
    END IF;

    -- 2) Entrada no validada (ingreso único).
    SELECT hora_validacion, id_evento, nombre_estadio, nombre_sector
    INTO v_validada, v_id_evento, v_estadio, v_sector
    FROM entrada
    WHERE nro_entrada = v_nro_entrada;

    IF v_validada IS NOT NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La entrada ya fue validada.';
    END IF;

    -- 3) El dispositivo pertenece al funcionario.
    IF NOT EXISTS (
        SELECT 1 FROM funcionario_dispositivo
        WHERE doc_funcionario = p_doc_funcionario AND id_dispositivo = p_id_dispositivo
    ) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El dispositivo no pertenece al funcionario.';
    END IF;

    -- 4) El funcionario está asignado al sector del evento de la entrada.
    IF NOT EXISTS (
        SELECT 1 FROM funcionario_asignado
        WHERE doc_funcionario = p_doc_funcionario
          AND id_evento       = v_id_evento
          AND nombre_estadio  = v_estadio
          AND nombre_sector   = v_sector
    ) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'El funcionario no está asignado a ese sector del evento.';
    END IF;

    START TRANSACTION;

    -- Registro de la validación (funcionario + código + dispositivo).
    INSERT INTO validacion(nro_entrada, id_codigo, doc_funcionario, id_dispositivo)
    VALUES (v_nro_entrada, v_id_codigo, p_doc_funcionario, p_id_dispositivo);

    -- Consume la entrada: queda validada (irreversible) y deja registrado el
    -- dispositivo que la validó.
    UPDATE entrada
    SET hora_validacion = NOW(6),
        id_dispositivo  = p_id_dispositivo
    WHERE nro_entrada = v_nro_entrada;

    -- Consume el código QR usado.
    UPDATE codigo_qr SET activo = FALSE WHERE id_codigo = v_id_codigo;

    COMMIT;

    SELECT v.nro_entrada, v_id_evento AS id_evento, v_estadio AS nombre_estadio,
           v_sector AS nombre_sector, v.fecha_hora, v.doc_funcionario, v.id_dispositivo
    FROM validacion v
    WHERE v.id_codigo = v_id_codigo;
END //

DELIMITER ;
