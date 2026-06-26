-- trigger que impide deshabilitar un sector con entradas vendidas.
-- Ninguna FK lo cubría: entrada referencia sector, no evento_sector.
-- Aplicar en la base viva: mysql -u ... ticketing < db/migrations/20260625_evento_sector_baja.sql

DROP TRIGGER IF EXISTS tr_evento_sector_del;

DELIMITER //

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

DELIMITER ;
