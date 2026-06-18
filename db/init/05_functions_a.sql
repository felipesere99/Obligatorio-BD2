-- ============================================================
--  05_functions_a.sql — Funciones de Persona A
--  (Identidad, Catálogo y Compra).
--
--  Se aplica en caliente sin perder datos:
--    docker compose exec -T db psql -U ticketing -d ticketing \
--      < db/init/05_functions_a.sql
-- ============================================================

-- ------------------------------------------------------------
--  A1 — Registro de usuarios
-- ------------------------------------------------------------

-- fn_registrar_usuario_general — alta de un usuario general (público).
-- Devuelve el documento creado. El UNIQUE de correo y la PK de documento
-- los traduce el middleware a 409.
CREATE OR REPLACE FUNCTION fn_registrar_usuario_general(
    p_documento         VARCHAR,
    p_nombre            VARCHAR,
    p_apellido          VARCHAR,
    p_correo            VARCHAR,
    p_dir_pais          VARCHAR DEFAULT NULL,
    p_dir_localidad     VARCHAR DEFAULT NULL,
    p_dir_calle         VARCHAR DEFAULT NULL,
    p_dir_numero        VARCHAR DEFAULT NULL,
    p_dir_codigo_postal VARCHAR DEFAULT NULL
)
RETURNS VARCHAR
LANGUAGE plpgsql
AS $$
BEGIN
    IF p_documento IS NULL OR btrim(p_documento) = '' THEN
        RAISE EXCEPTION 'El documento es obligatorio.';
    END IF;
    IF p_nombre IS NULL OR btrim(p_nombre) = '' THEN
        RAISE EXCEPTION 'El nombre es obligatorio.';
    END IF;
    IF p_apellido IS NULL OR btrim(p_apellido) = '' THEN
        RAISE EXCEPTION 'El apellido es obligatorio.';
    END IF;
    IF p_correo IS NULL OR btrim(p_correo) = '' THEN
        RAISE EXCEPTION 'El correo es obligatorio.';
    END IF;

    INSERT INTO usuario_general(
        documento, nombre, apellido, correo,
        dir_pais, dir_localidad, dir_calle, dir_numero, dir_codigo_postal
    ) VALUES (
        btrim(p_documento), btrim(p_nombre), btrim(p_apellido), btrim(p_correo),
        p_dir_pais, p_dir_localidad, p_dir_calle, p_dir_numero, p_dir_codigo_postal
    );

    RETURN btrim(p_documento);
END;
$$;
