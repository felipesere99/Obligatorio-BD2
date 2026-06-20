-- ============================================================
--  05_functions_a.sql — Procedimientos de Persona A
--  (Identidad, Catálogo y Compra). Dialecto: MySQL 8.
--
--  Se aplica en caliente sin perder datos:
--    docker compose exec -T db \
--      mysql -uticketing -pticketing ticketing < db/init/05_functions_a.sql
-- ============================================================

DROP PROCEDURE IF EXISTS sp_registrar_usuario_general;
DROP PROCEDURE IF EXISTS sp_registrar_equipo;
DROP PROCEDURE IF EXISTS sp_registrar_estadio;
DROP PROCEDURE IF EXISTS sp_registrar_sector;
DROP PROCEDURE IF EXISTS sp_crear_evento;
DROP PROCEDURE IF EXISTS sp_habilitar_sector;
DROP PROCEDURE IF EXISTS sp_comision_vigente;
DROP PROCEDURE IF EXISTS sp_set_comision;
DROP PROCEDURE IF EXISTS sp_crear_venta;

DELIMITER //

-- ------------------------------------------------------------
--  A1 — Registro de usuarios
-- ------------------------------------------------------------
-- sp_registrar_usuario_general — alta de un usuario general (público).
-- Devuelve el documento creado. El UNIQUE de correo y la PK de documento
-- los traduce el middleware a 409.
CREATE PROCEDURE sp_registrar_usuario_general(
    IN p_documento         VARCHAR(30),
    IN p_nombre            VARCHAR(100),
    IN p_apellido          VARCHAR(100),
    IN p_correo            VARCHAR(254),
    IN p_dir_pais          VARCHAR(60),
    IN p_dir_localidad     VARCHAR(80),
    IN p_dir_calle         VARCHAR(120),
    IN p_dir_numero        VARCHAR(20),
    IN p_dir_codigo_postal VARCHAR(12)
)
BEGIN
    IF p_documento IS NULL OR TRIM(p_documento) = '' THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El documento es obligatorio.';
    END IF;
    IF p_nombre IS NULL OR TRIM(p_nombre) = '' THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El nombre es obligatorio.';
    END IF;
    IF p_apellido IS NULL OR TRIM(p_apellido) = '' THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El apellido es obligatorio.';
    END IF;
    IF p_correo IS NULL OR TRIM(p_correo) = '' THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El correo es obligatorio.';
    END IF;

    INSERT INTO usuario_general(
        documento, nombre, apellido, correo,
        dir_pais, dir_localidad, dir_calle, dir_numero, dir_codigo_postal
    ) VALUES (
        TRIM(p_documento), TRIM(p_nombre), TRIM(p_apellido), TRIM(p_correo),
        p_dir_pais, p_dir_localidad, p_dir_calle, p_dir_numero, p_dir_codigo_postal
    );

    SELECT TRIM(p_documento) AS documento;
END //

-- ------------------------------------------------------------
--  A2 — Equipos
-- ------------------------------------------------------------
-- sp_registrar_equipo — alta de un equipo (PK = país). Devuelve el país.
CREATE PROCEDURE sp_registrar_equipo(
    IN p_pais   VARCHAR(60),
    IN p_nombre VARCHAR(80)
)
BEGIN
    IF p_pais IS NULL OR TRIM(p_pais) = '' THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El país es obligatorio.';
    END IF;
    IF p_nombre IS NULL OR TRIM(p_nombre) = '' THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El nombre es obligatorio.';
    END IF;

    INSERT INTO equipo(pais, nombre)
    VALUES (TRIM(p_pais), TRIM(p_nombre));

    SELECT TRIM(p_pais) AS pais;
END //

-- ------------------------------------------------------------
--  A3 — Estadios + Sectores
-- ------------------------------------------------------------
-- sp_registrar_estadio — alta de un estadio (PK = nombre). Devuelve el nombre.
CREATE PROCEDURE sp_registrar_estadio(
    IN p_nombre    VARCHAR(120),
    IN p_direccion VARCHAR(200)
)
BEGIN
    IF p_nombre IS NULL OR TRIM(p_nombre) = '' THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El nombre del estadio es obligatorio.';
    END IF;

    INSERT INTO estadio(nombre, direccion)
    VALUES (TRIM(p_nombre), p_direccion);

    SELECT TRIM(p_nombre) AS nombre;
END //

-- sp_registrar_sector — alta de un sector de un estadio (entidad débil).
-- Devuelve el nombre del sector. Si el estadio no existe, la FK lo frena (400);
-- el nombre repetido en el mismo estadio lo frena la PK (409).
CREATE PROCEDURE sp_registrar_sector(
    IN p_nombre_estadio VARCHAR(120),
    IN p_nombre         VARCHAR(80),
    IN p_capacidad      INT,
    IN p_costo_entrada  DECIMAL(12,2)
)
BEGIN
    IF p_nombre_estadio IS NULL OR TRIM(p_nombre_estadio) = '' THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El estadio es obligatorio.';
    END IF;
    IF p_nombre IS NULL OR TRIM(p_nombre) = '' THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El nombre del sector es obligatorio.';
    END IF;
    IF p_capacidad IS NULL OR p_capacidad <= 0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La capacidad debe ser mayor a cero.';
    END IF;
    IF p_costo_entrada IS NULL OR p_costo_entrada < 0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El costo de entrada no puede ser negativo.';
    END IF;

    INSERT INTO sector(nombre_estadio, nombre, capacidad, costo_entrada)
    VALUES (TRIM(p_nombre_estadio), TRIM(p_nombre), p_capacidad, p_costo_entrada);

    SELECT TRIM(p_nombre) AS nombre;
END //

-- ------------------------------------------------------------
--  A4 — Eventos + habilitar sectores
-- ------------------------------------------------------------
-- sp_crear_evento — alta de un evento. Devuelve el id generado.
-- La superposición en el mismo estadio la frena el trigger
-- tr_evento_superpos_ins (-> 400); equipos/estadio inexistentes, la FK (-> 400).
CREATE PROCEDURE sp_crear_evento(
    IN p_nombre         VARCHAR(150),
    IN p_fecha_inicio   DATETIME(6),
    IN p_fecha_fin      DATETIME(6),
    IN p_pais_local     VARCHAR(60),
    IN p_pais_visitante VARCHAR(60),
    IN p_nombre_estadio VARCHAR(120)
)
BEGIN
    DECLARE v_id INT;

    IF p_nombre IS NULL OR TRIM(p_nombre) = '' THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El nombre del evento es obligatorio.';
    END IF;
    IF p_fecha_inicio IS NULL OR p_fecha_fin IS NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Las fechas de inicio y fin son obligatorias.';
    END IF;
    IF p_fecha_fin <= p_fecha_inicio THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La fecha de fin debe ser posterior a la de inicio.';
    END IF;
    IF p_nombre_estadio IS NULL OR TRIM(p_nombre_estadio) = '' THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El estadio es obligatorio.';
    END IF;

    INSERT INTO evento(nombre, fecha_inicio, fecha_fin, pais_local, pais_visitante, nombre_estadio)
    VALUES (TRIM(p_nombre), p_fecha_inicio, p_fecha_fin,
            p_pais_local, p_pais_visitante, TRIM(p_nombre_estadio));

    SET v_id = LAST_INSERT_ID();
    SELECT v_id AS id_evento;
END //

-- sp_habilitar_sector — habilita un sector para un evento. Devuelve el sector.
-- Que el sector pertenezca al estadio del evento lo frena el trigger
-- tr_evento_sector_ins (-> 400); el sector inexistente, la FK (-> 400);
-- el duplicado, la PK (-> 409).
CREATE PROCEDURE sp_habilitar_sector(
    IN p_id_evento      INT,
    IN p_nombre_estadio VARCHAR(120),
    IN p_nombre_sector  VARCHAR(80)
)
BEGIN
    IF p_id_evento IS NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El evento es obligatorio.';
    END IF;
    IF p_nombre_estadio IS NULL OR TRIM(p_nombre_estadio) = '' THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El estadio es obligatorio.';
    END IF;
    IF p_nombre_sector IS NULL OR TRIM(p_nombre_sector) = '' THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El sector es obligatorio.';
    END IF;

    INSERT INTO evento_sector(id_evento, nombre_estadio, nombre_sector)
    VALUES (p_id_evento, TRIM(p_nombre_estadio), TRIM(p_nombre_sector));

    SELECT TRIM(p_nombre_sector) AS nombre_sector;
END //

-- ------------------------------------------------------------
--  A5 — Comisión (vigencia temporal)
-- ------------------------------------------------------------
-- sp_comision_vigente — devuelve la comisión vigente (vigente_hasta IS NULL).
CREATE PROCEDURE sp_comision_vigente()
BEGIN
    SELECT c.id_comision, c.porcentaje, c.vigente_desde
    FROM comision c
    WHERE c.vigente_hasta IS NULL;
END //

-- sp_set_comision — cierra la comisión vigente (vigente_hasta = now()) y abre
-- una nueva. Devuelve la comisión recién creada (ya vigente).
CREATE PROCEDURE sp_set_comision(IN p_porcentaje DECIMAL(5,2))
BEGIN
    DECLARE v_id INT;

    IF p_porcentaje IS NULL OR p_porcentaje < 0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El porcentaje debe ser mayor o igual a cero.';
    END IF;

    UPDATE comision SET vigente_hasta = NOW(6) WHERE vigente_hasta IS NULL;

    INSERT INTO comision(porcentaje, vigente_desde)
    VALUES (p_porcentaje, NOW(6));
    SET v_id = LAST_INSERT_ID();

    SELECT c.id_comision, c.porcentaje, c.vigente_desde
    FROM comision c
    WHERE c.id_comision = v_id;
END //

-- ------------------------------------------------------------
--  A6 — Compra (capstone)
-- ------------------------------------------------------------
-- sp_crear_venta — crea una venta a partir de un array JSON de items
-- [{ id_evento, estadio, sector, fila, asiento }, ...].
--   * monto = Σ costo_entrada × (1 + %comisión_vigente / 100)
--   * inserta la venta (con la comisión vigente) y una entrada por item.
--   * los triggers de 02 refuerzan: sector habilitado, capacidad y ≤ 5/venta.
-- Devuelve nro_venta y monto_total. Es atómica: el EXIT HANDLER hace ROLLBACK
-- y re-lanza el error si una entrada falla, revirtiendo toda la venta.
CREATE PROCEDURE sp_crear_venta(
    IN p_comprador VARCHAR(30),
    IN p_items     JSON
)
BEGIN
    DECLARE v_id_comision INT;
    DECLARE v_pct         DECIMAL(5,2);
    DECLARE v_cant        INT;
    DECLARE v_monto       DECIMAL(14,2);
    DECLARE v_nro         INT;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    IF p_items IS NULL OR JSON_TYPE(p_items) <> 'ARRAY' THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Los items de la compra son obligatorios.';
    END IF;

    SET v_cant = JSON_LENGTH(p_items);
    IF v_cant = 0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La compra no tiene entradas.';
    END IF;
    IF v_cant > 5 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Una venta admite como máximo 5 entradas.';
    END IF;

    -- Comisión vigente.
    SELECT id_comision, porcentaje INTO v_id_comision, v_pct
    FROM comision WHERE vigente_hasta IS NULL LIMIT 1;
    IF v_id_comision IS NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'No hay una comisión vigente.';
    END IF;

    -- Monto: suma de costos de los sectores pedidos, más la comisión.
    SELECT COALESCE(SUM(s.costo_entrada), 0) * (1 + v_pct / 100)
    INTO v_monto
    FROM JSON_TABLE(p_items, '$[*]' COLUMNS (
        estadio VARCHAR(120) PATH '$.estadio',
        sector  VARCHAR(80)  PATH '$.sector'
    )) jt
    JOIN sector s
      ON s.nombre_estadio = jt.estadio
     AND s.nombre         = jt.sector;

    START TRANSACTION;

    -- Cabecera de la venta (con la comisión vigente).
    INSERT INTO venta(monto_total, estado, doc_comprador, id_comision)
    VALUES (ROUND(v_monto, 2), 'pendiente', p_comprador, v_id_comision);
    SET v_nro = LAST_INSERT_ID();

    -- Una entrada por item; los triggers validan cada inserción.
    INSERT INTO entrada(
        nro_venta, id_evento, nombre_estadio, nombre_sector,
        fila, asiento, doc_propietario
    )
    SELECT v_nro, jt.id_evento, jt.estadio, jt.sector, jt.fila, jt.asiento, p_comprador
    FROM JSON_TABLE(p_items, '$[*]' COLUMNS (
        id_evento INT          PATH '$.id_evento',
        estadio   VARCHAR(120) PATH '$.estadio',
        sector    VARCHAR(80)  PATH '$.sector',
        fila      VARCHAR(10)  PATH '$.fila',
        asiento   VARCHAR(10)  PATH '$.asiento'
    )) jt;

    COMMIT;

    SELECT v.nro_venta, v.monto_total FROM venta v WHERE v.nro_venta = v_nro;
END //

DELIMITER ;
