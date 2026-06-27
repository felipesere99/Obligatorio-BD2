-- Fase 1: rechazar que un equipo juegue dos partidos solapados en cualquier estadio.
-- Aplicar en la base viva: mysql -u ... ticketing < db/migrations/20260625_evento_superpos_equipo.sql

DROP TRIGGER IF EXISTS tr_evento_superpos_ins;
DROP TRIGGER IF EXISTS tr_evento_superpos_upd;

DELIMITER //

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

DELIMITER ;
