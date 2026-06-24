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
