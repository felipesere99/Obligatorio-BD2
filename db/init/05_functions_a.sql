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

-- ------------------------------------------------------------
--  A2 — Equipos
-- ------------------------------------------------------------

-- fn_registrar_equipo — alta de un equipo (PK = país). Devuelve el país.
-- El país duplicado lo frena la PK y el middleware lo traduce a 409.
CREATE OR REPLACE FUNCTION fn_registrar_equipo(
    p_pais   VARCHAR,
    p_nombre VARCHAR
)
RETURNS VARCHAR
LANGUAGE plpgsql
AS $$
BEGIN
    IF p_pais IS NULL OR btrim(p_pais) = '' THEN
        RAISE EXCEPTION 'El país es obligatorio.';
    END IF;
    IF p_nombre IS NULL OR btrim(p_nombre) = '' THEN
        RAISE EXCEPTION 'El nombre es obligatorio.';
    END IF;

    INSERT INTO equipo(pais, nombre)
    VALUES (btrim(p_pais), btrim(p_nombre));

    RETURN btrim(p_pais);
END;
$$;

-- ------------------------------------------------------------
--  A3 — Estadios + Sectores
-- ------------------------------------------------------------

-- fn_registrar_estadio — alta de un estadio (PK = nombre). Devuelve el nombre.
CREATE OR REPLACE FUNCTION fn_registrar_estadio(
    p_nombre    VARCHAR,
    p_direccion VARCHAR DEFAULT NULL
)
RETURNS VARCHAR
LANGUAGE plpgsql
AS $$
BEGIN
    IF p_nombre IS NULL OR btrim(p_nombre) = '' THEN
        RAISE EXCEPTION 'El nombre del estadio es obligatorio.';
    END IF;

    INSERT INTO estadio(nombre, direccion)
    VALUES (btrim(p_nombre), p_direccion);

    RETURN btrim(p_nombre);
END;
$$;

-- fn_registrar_sector — alta de un sector de un estadio (entidad débil).
-- Devuelve el nombre del sector. Si el estadio no existe, la FK lo frena (400);
-- el nombre repetido en el mismo estadio lo frena la PK (409).
CREATE OR REPLACE FUNCTION fn_registrar_sector(
    p_nombre_estadio VARCHAR,
    p_nombre         VARCHAR,
    p_capacidad      INT,
    p_costo_entrada  NUMERIC
)
RETURNS VARCHAR
LANGUAGE plpgsql
AS $$
BEGIN
    IF p_nombre_estadio IS NULL OR btrim(p_nombre_estadio) = '' THEN
        RAISE EXCEPTION 'El estadio es obligatorio.';
    END IF;
    IF p_nombre IS NULL OR btrim(p_nombre) = '' THEN
        RAISE EXCEPTION 'El nombre del sector es obligatorio.';
    END IF;
    IF p_capacidad IS NULL OR p_capacidad <= 0 THEN
        RAISE EXCEPTION 'La capacidad debe ser mayor a cero.';
    END IF;
    IF p_costo_entrada IS NULL OR p_costo_entrada < 0 THEN
        RAISE EXCEPTION 'El costo de entrada no puede ser negativo.';
    END IF;

    INSERT INTO sector(nombre_estadio, nombre, capacidad, costo_entrada)
    VALUES (btrim(p_nombre_estadio), btrim(p_nombre), p_capacidad, p_costo_entrada);

    RETURN btrim(p_nombre);
END;
$$;
