-- ============================================================
--  00_ping.sql — Función de salud mínima (health check).
-- ============================================================
DROP FUNCTION IF EXISTS fn_ping;

DELIMITER //
CREATE FUNCTION fn_ping() RETURNS VARCHAR(10)
DETERMINISTIC NO SQL
BEGIN
    RETURN 'pong';
END //
DELIMITER ;
