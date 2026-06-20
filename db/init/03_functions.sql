-- ============================================================
--  03_functions.sql — Procedimientos base compartidos.
--  Cada persona agrega los suyos en archivos 05_*, 06_*...
--  Dialecto: MySQL 8. Las funciones que devolvían TABLE en PostgreSQL
--  pasan a ser PROCEDURES (MySQL no permite funciones que devuelvan tablas
--  ni, con binlog activo, funciones que modifiquen datos).
-- ============================================================

-- ------------------------------------------------------------
--  sp_login — resuelve el rol de un documento buscándolo en las
--  tres tablas de rol. Lanza excepción (SIGNAL) si no existe.
-- ------------------------------------------------------------
DROP PROCEDURE IF EXISTS sp_login;

DELIMITER //
CREATE PROCEDURE sp_login(IN p_documento VARCHAR(30))
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM administrador   WHERE documento = p_documento
        UNION ALL
        SELECT 1 FROM funcionario     WHERE documento = p_documento
        UNION ALL
        SELECT 1 FROM usuario_general WHERE documento = p_documento
    ) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'No existe un usuario con ese documento';
    END IF;

    SELECT a.documento AS documento, 'administrador' AS rol, a.nombre AS nombre
        FROM administrador a WHERE a.documento = p_documento
    UNION ALL
    SELECT f.documento, 'funcionario', f.nombre
        FROM funcionario f WHERE f.documento = p_documento
    UNION ALL
    SELECT u.documento, 'usuario_general', u.nombre
        FROM usuario_general u WHERE u.documento = p_documento;
END //
DELIMITER ;
