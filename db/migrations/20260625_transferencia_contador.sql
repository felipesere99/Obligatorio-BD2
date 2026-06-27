-- el campo `contador` debe reflejar el número de orden de las
-- transferencias *aceptadas*, no el total de filas (incluidas canceladas/rechazadas).
-- Sin este fix, crear+cancelar varias veces hace que `v_existentes + 1` supere 3
-- y choque con CHECK (contador BETWEEN 1 AND 3) antes de llegar a 3 aceptadas.
-- Aplicar en la base viva: mysql -u ... ticketing < db/migrations/20260625_transferencia_contador.sql

DROP TRIGGER IF EXISTS tr_transferencia_control;

DELIMITER //

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

DELIMITER ;
