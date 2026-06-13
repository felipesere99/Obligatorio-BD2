-- ============================================================
--  02_triggers.sql — Reglas de negocio que no se pueden expresar
--  con constraints declarativos (referenciadas desde 01_schema.sql).
-- ============================================================

-- ------------------------------------------------------------
-- 1) No superposición de eventos en un mismo estadio.
-- ------------------------------------------------------------
CREATE OR REPLACE FUNCTION trg_evento_sin_superposicion()
RETURNS TRIGGER AS $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM evento e
        WHERE e.nombre_estadio = NEW.nombre_estadio
          AND e.id_evento     <> NEW.id_evento
          AND NEW.fecha_inicio < e.fecha_fin
          AND NEW.fecha_fin    > e.fecha_inicio
    ) THEN
        RAISE EXCEPTION 'Superposición de eventos en el estadio % en el rango [%, %]',
            NEW.nombre_estadio, NEW.fecha_inicio, NEW.fecha_fin;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER tr_evento_sin_superposicion
    BEFORE INSERT OR UPDATE ON evento
    FOR EACH ROW EXECUTE FUNCTION trg_evento_sin_superposicion();

-- ------------------------------------------------------------
-- 2) Un sector habilitado para un evento debe pertenecer al
--    estadio donde se juega ese evento.
-- ------------------------------------------------------------
CREATE OR REPLACE FUNCTION trg_evento_sector_mismo_estadio()
RETURNS TRIGGER AS $$
DECLARE
    v_estadio_evento VARCHAR(120);
BEGIN
    SELECT nombre_estadio INTO v_estadio_evento FROM evento WHERE id_evento = NEW.id_evento;
    IF v_estadio_evento <> NEW.nombre_estadio THEN
        RAISE EXCEPTION 'El sector (estadio %) no pertenece al estadio % del evento %',
            NEW.nombre_estadio, v_estadio_evento, NEW.id_evento;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER tr_evento_sector_mismo_estadio
    BEFORE INSERT OR UPDATE ON evento_sector
    FOR EACH ROW EXECUTE FUNCTION trg_evento_sector_mismo_estadio();

-- ------------------------------------------------------------
-- 3) Una entrada solo puede emitirse para un sector HABILITADO
--    para ese evento (garantiza también estadio coherente).
-- ------------------------------------------------------------
CREATE OR REPLACE FUNCTION trg_entrada_sector_habilitado()
RETURNS TRIGGER AS $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM evento_sector es
        WHERE es.id_evento      = NEW.id_evento
          AND es.nombre_estadio = NEW.nombre_estadio
          AND es.nombre_sector  = NEW.nombre_sector
    ) THEN
        RAISE EXCEPTION 'El sector %/% no está habilitado para el evento %',
            NEW.nombre_estadio, NEW.nombre_sector, NEW.id_evento;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER tr_entrada_sector_habilitado
    BEFORE INSERT ON entrada
    FOR EACH ROW EXECUTE FUNCTION trg_entrada_sector_habilitado();

-- ------------------------------------------------------------
-- 4) Máximo 5 entradas por venta.
-- ------------------------------------------------------------
CREATE OR REPLACE FUNCTION trg_max_entradas_por_venta()
RETURNS TRIGGER AS $$
DECLARE
    v_cant INT;
BEGIN
    SELECT count(*) INTO v_cant FROM entrada WHERE nro_venta = NEW.nro_venta;
    IF v_cant >= 5 THEN
        RAISE EXCEPTION 'La venta % ya tiene 5 entradas (máximo permitido)', NEW.nro_venta;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER tr_max_entradas_por_venta
    BEFORE INSERT ON entrada
    FOR EACH ROW EXECUTE FUNCTION trg_max_entradas_por_venta();

-- ------------------------------------------------------------
-- 5) Capacidad por sector (límite duro) para un mismo evento.
-- ------------------------------------------------------------
CREATE OR REPLACE FUNCTION trg_capacidad_sector()
RETURNS TRIGGER AS $$
DECLARE
    v_capacidad INT;
    v_vendidas  INT;
BEGIN
    SELECT capacidad INTO v_capacidad
    FROM sector
    WHERE nombre_estadio = NEW.nombre_estadio AND nombre = NEW.nombre_sector;

    SELECT count(*) INTO v_vendidas
    FROM entrada
    WHERE id_evento      = NEW.id_evento
      AND nombre_estadio = NEW.nombre_estadio
      AND nombre_sector  = NEW.nombre_sector;

    IF v_vendidas >= v_capacidad THEN
        RAISE EXCEPTION 'Capacidad agotada en el sector % del estadio % para el evento %',
            NEW.nombre_sector, NEW.nombre_estadio, NEW.id_evento;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER tr_capacidad_sector
    BEFORE INSERT ON entrada
    FOR EACH ROW EXECUTE FUNCTION trg_capacidad_sector();

-- ------------------------------------------------------------
-- 6) Validación irreversible: una vez registrada la hora_validacion
--    no puede volver a NULL.
-- ------------------------------------------------------------
CREATE OR REPLACE FUNCTION trg_validacion_irreversible()
RETURNS TRIGGER AS $$
BEGIN
    IF OLD.hora_validacion IS NOT NULL AND NEW.hora_validacion IS NULL THEN
        RAISE EXCEPTION 'La validación de la entrada % es irreversible', OLD.nro_entrada;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER tr_validacion_irreversible
    BEFORE UPDATE ON entrada
    FOR EACH ROW EXECUTE FUNCTION trg_validacion_irreversible();

-- ------------------------------------------------------------
-- 7) Transferencia: máx. 3 por entrada, no transferir una entrada
--    ya validada, y numerar el contador en la cadena de custodia.
-- ------------------------------------------------------------
CREATE OR REPLACE FUNCTION trg_transferencia_control()
RETURNS TRIGGER AS $$
DECLARE
    v_validada    TIMESTAMPTZ;
    v_aceptadas   INT;
    v_existentes  INT;
BEGIN
    SELECT hora_validacion INTO v_validada FROM entrada WHERE nro_entrada = NEW.nro_entrada;
    IF v_validada IS NOT NULL THEN
        RAISE EXCEPTION 'La entrada % ya fue validada: no admite transferencias', NEW.nro_entrada;
    END IF;

    SELECT count(*) INTO v_aceptadas
    FROM transferencia
    WHERE nro_entrada = NEW.nro_entrada AND estado = 'aceptada';
    IF v_aceptadas >= 3 THEN
        RAISE EXCEPTION 'La entrada % ya alcanzó el máximo de 3 transferencias', NEW.nro_entrada;
    END IF;

    SELECT count(*) INTO v_existentes
    FROM transferencia
    WHERE nro_entrada = NEW.nro_entrada;
    NEW.contador := v_existentes + 1;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER tr_transferencia_control
    BEFORE INSERT ON transferencia
    FOR EACH ROW EXECUTE FUNCTION trg_transferencia_control();

-- ------------------------------------------------------------
-- 8) Tenencia inicial: al emitir una entrada, su propietario la
--    "tiene" (mantiene usuario_tiene_entradas poblada).
-- ------------------------------------------------------------
CREATE OR REPLACE FUNCTION trg_entrada_tenencia_inicial()
RETURNS TRIGGER AS $$
BEGIN
    INSERT INTO usuario_tiene_entradas (documento_usuario, nro_entrada)
    VALUES (NEW.doc_propietario, NEW.nro_entrada);
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER tr_entrada_tenencia_inicial
    AFTER INSERT ON entrada
    FOR EACH ROW EXECUTE FUNCTION trg_entrada_tenencia_inicial();

-- ------------------------------------------------------------
-- 9) Aceptación de transferencia: cambia el propietario de la
--    entrada y actualiza la tenencia (emisor -> receptor).
-- ------------------------------------------------------------
CREATE OR REPLACE FUNCTION trg_transferencia_aceptada()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW.estado = 'aceptada' AND OLD.estado <> 'aceptada' THEN
        UPDATE entrada
        SET doc_propietario = NEW.doc_receptor
        WHERE nro_entrada = NEW.nro_entrada;

        DELETE FROM usuario_tiene_entradas
        WHERE nro_entrada = NEW.nro_entrada AND documento_usuario = NEW.doc_emisor;

        INSERT INTO usuario_tiene_entradas (documento_usuario, nro_entrada)
        VALUES (NEW.doc_receptor, NEW.nro_entrada)
        ON CONFLICT DO NOTHING;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER tr_transferencia_aceptada
    AFTER UPDATE ON transferencia
    FOR EACH ROW EXECUTE FUNCTION trg_transferencia_aceptada();
