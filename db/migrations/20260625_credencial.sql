-- ============================================================
--  20260625_credencial.sql — Tabla de credenciales (contraseñas)
--  Aplica a bases de datos ya existentes (IF NOT EXISTS).
-- ============================================================

CREATE TABLE IF NOT EXISTS credencial (
    documento      VARCHAR(30)  PRIMARY KEY,
    hash           VARCHAR(100) NOT NULL,
    actualizado_en DATETIME(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6)
);
