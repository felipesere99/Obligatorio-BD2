-- ============================================================
--  03_functions.sql — Funciones base compartidas.
--  Cada persona agrega sus propias funciones en archivos 05_*, 06_*...
-- ============================================================

-- ------------------------------------------------------------
--  fn_login — resuelve el rol de un documento buscándolo en las
--  tres tablas de rol. Lanza excepción si no existe.
-- ------------------------------------------------------------
CREATE OR REPLACE FUNCTION fn_login(p_documento VARCHAR)
RETURNS TABLE(documento VARCHAR, rol TEXT, nombre VARCHAR)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY
        SELECT a.documento, 'administrador'::text,   a.nombre FROM administrador   a WHERE a.documento = p_documento
        UNION ALL
        SELECT f.documento, 'funcionario'::text,     f.nombre FROM funcionario     f WHERE f.documento = p_documento
        UNION ALL
        SELECT u.documento, 'usuario_general'::text, u.nombre FROM usuario_general u WHERE u.documento = p_documento;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'No existe un usuario con documento %', p_documento;
    END IF;
END;
$$;
