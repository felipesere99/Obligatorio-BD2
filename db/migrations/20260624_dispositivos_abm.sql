-- Migracion para bases existentes que ya tenian la tabla dispositivo
-- creada solamente con id_dispositivo.
--
-- Ejecutar una sola vez sobre la base viva antes de usar el ABM nuevo:
--   mysql -h <host> -P <puerto> -u <usuario> -p <base> < db/migrations/20260624_dispositivos_abm.sql

ALTER TABLE dispositivo
    ADD COLUMN nro_serie VARCHAR(80) NULL,
    ADD COLUMN marca VARCHAR(80) NULL,
    ADD COLUMN modelo VARCHAR(80) NULL,
    ADD COLUMN habilitado BOOLEAN NOT NULL DEFAULT TRUE;

UPDATE dispositivo
SET nro_serie = CONCAT('LEGACY-', LPAD(id_dispositivo, 4, '0')),
    marca = 'Sin marca',
    modelo = 'Sin modelo'
WHERE nro_serie IS NULL;

ALTER TABLE dispositivo
    MODIFY nro_serie VARCHAR(80) NOT NULL,
    MODIFY marca VARCHAR(80) NOT NULL,
    MODIFY modelo VARCHAR(80) NOT NULL,
    ADD CONSTRAINT uq_dispositivo_nro_serie UNIQUE (nro_serie);

DROP TRIGGER IF EXISTS tr_validacion_dispositivo;

DELIMITER //

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

DELIMITER ;
