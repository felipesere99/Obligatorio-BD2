-- ============================================================
--  01_schema.sql — Esquema relacional (Ticketing — Mundial 2026)
--  Dialecto: MySQL 8.
--  Notas de portabilidad desde el modelo original (PostgreSQL):
--   * SERIAL            -> INT AUTO_INCREMENT
--   * TIMESTAMPTZ       -> DATETIME(6) (se guarda UTC por convención)
--   * NUMERIC           -> DECIMAL
--   * REFERENCES inline -> FOREIGN KEY a nivel tabla (MySQL ignora las inline)
--   * Índices parciales (WHERE) -> UNIQUE normal (NULLs distintos) o columna generada
-- ============================================================

-- ---------- Dispositivos de validación ----------
CREATE TABLE dispositivo (
    id_dispositivo INT AUTO_INCREMENT PRIMARY KEY,
    nro_serie      VARCHAR(80)  NOT NULL UNIQUE,
    marca          VARCHAR(80)  NOT NULL,
    modelo         VARCHAR(80)  NOT NULL,
    habilitado     BOOLEAN      NOT NULL DEFAULT TRUE
);

-- ---------- Rol: Administrador ----------
CREATE TABLE administrador (
    documento         VARCHAR(30)  PRIMARY KEY,
    nombre            VARCHAR(100) NOT NULL,
    apellido          VARCHAR(100) NOT NULL,
    correo            VARCHAR(254) NOT NULL UNIQUE,
    dir_pais          VARCHAR(60),
    dir_localidad     VARCHAR(80),
    dir_calle         VARCHAR(120),
    dir_numero        VARCHAR(20),
    dir_codigo_postal VARCHAR(12),
    fecha             DATE         NOT NULL DEFAULT (CURRENT_DATE),
    cargo             VARCHAR(80)  NOT NULL
);

-- ---------- Rol: Funcionario ----------
CREATE TABLE funcionario (
    documento         VARCHAR(30)  PRIMARY KEY,
    nombre            VARCHAR(100) NOT NULL,
    apellido          VARCHAR(100) NOT NULL,
    correo            VARCHAR(254) NOT NULL UNIQUE,
    dir_pais          VARCHAR(60),
    dir_localidad     VARCHAR(80),
    dir_calle         VARCHAR(120),
    dir_numero        VARCHAR(20),
    dir_codigo_postal VARCHAR(12),
    nro_legajo        VARCHAR(30)  NOT NULL UNIQUE
);

-- ---------- Rol: UsuarioGral ----------
CREATE TABLE usuario_general (
    documento           VARCHAR(30)  PRIMARY KEY,
    nombre              VARCHAR(100) NOT NULL,
    apellido            VARCHAR(100) NOT NULL,
    correo              VARCHAR(254) NOT NULL UNIQUE,
    dir_pais            VARCHAR(60),
    dir_localidad       VARCHAR(80),
    dir_calle           VARCHAR(120),
    dir_numero          VARCHAR(20),
    dir_codigo_postal   VARCHAR(12),
    fecha_alta          DATE         NOT NULL DEFAULT (CURRENT_DATE),
    estado_verificacion BOOLEAN      NOT NULL DEFAULT FALSE
);

-- ---------- TelefonoUsuario (atributo multivaluado) ----------
-- Al no existir supertipo Usuario, el documento puede pertenecer a
-- cualquiera de los tres roles; no hay una única tabla a referenciar,
-- por lo que documento queda sin FK declarativa.
CREATE TABLE telefono_usuario (
    documento VARCHAR(30) NOT NULL,
    telefono  VARCHAR(30) NOT NULL,
    PRIMARY KEY (documento, telefono)
);

-- ---------- Equipos ----------
CREATE TABLE equipo (
    pais   VARCHAR(60) PRIMARY KEY,
    nombre VARCHAR(80) NOT NULL
);

-- ---------- Estadios ----------
CREATE TABLE estadio (
    nombre    VARCHAR(120) PRIMARY KEY,
    direccion VARCHAR(200)
);

-- ---------- Sectores (entidad débil de Estadio) ----------
CREATE TABLE sector (
    nombre_estadio VARCHAR(120)  NOT NULL,
    nombre         VARCHAR(80)   NOT NULL,
    capacidad      INT           NOT NULL CHECK (capacidad > 0),
    costo_entrada  DECIMAL(12,2) NOT NULL CHECK (costo_entrada >= 0),
    PRIMARY KEY (nombre_estadio, nombre),
    CONSTRAINT fk_sector_estadio FOREIGN KEY (nombre_estadio)
        REFERENCES estadio(nombre) ON DELETE CASCADE
);

-- ---------- Eventos ----------
CREATE TABLE evento (
    id_evento       INT          AUTO_INCREMENT PRIMARY KEY,
    nombre          VARCHAR(150) NOT NULL,
    fecha_inicio    DATETIME(6)  NOT NULL,
    fecha_fin       DATETIME(6)  NOT NULL,
    pais_local      VARCHAR(60),
    pais_visitante  VARCHAR(60),
    nombre_estadio  VARCHAR(120) NOT NULL,
    CONSTRAINT ck_evento_fechas  CHECK (fecha_fin > fecha_inicio),
    -- pais_local distinto de pais_visitante (<=> es el igual null-safe de MySQL)
    CONSTRAINT ck_evento_equipos CHECK (NOT (pais_local <=> pais_visitante)),
    CONSTRAINT fk_evento_local     FOREIGN KEY (pais_local)     REFERENCES equipo(pais),
    CONSTRAINT fk_evento_visitante FOREIGN KEY (pais_visitante) REFERENCES equipo(pais),
    CONSTRAINT fk_evento_estadio   FOREIGN KEY (nombre_estadio) REFERENCES estadio(nombre)
    -- No superposición de eventos en un mismo estadio: trigger en 02
);

-- ---------- Sectores habilitados por evento ----------
CREATE TABLE evento_sector (
    id_evento      INT          NOT NULL,
    nombre_estadio VARCHAR(120) NOT NULL,
    nombre_sector  VARCHAR(80)  NOT NULL,
    PRIMARY KEY (id_evento, nombre_estadio, nombre_sector),
    CONSTRAINT fk_es_evento FOREIGN KEY (id_evento) REFERENCES evento(id_evento) ON DELETE CASCADE,
    CONSTRAINT fk_es_sector FOREIGN KEY (nombre_estadio, nombre_sector)
        REFERENCES sector(nombre_estadio, nombre)
);

-- ---------- Comisión variable en el tiempo ----------
CREATE TABLE comision (
    id_comision   INT          AUTO_INCREMENT PRIMARY KEY,
    porcentaje    DECIMAL(5,2) NOT NULL CHECK (porcentaje >= 0),
    vigente_desde DATETIME(6)  NOT NULL,
    vigente_hasta DATETIME(6),
    CONSTRAINT ck_comision_rango CHECK (vigente_hasta IS NULL OR vigente_hasta > vigente_desde)
);

-- ---------- Ventas ----------
CREATE TABLE venta (
    nro_venta     INT           AUTO_INCREMENT PRIMARY KEY,
    monto_total   DECIMAL(14,2) NOT NULL CHECK (monto_total >= 0),
    estado        VARCHAR(15)   NOT NULL DEFAULT 'pendiente',
    fecha         DATETIME(6)   NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    doc_comprador VARCHAR(30)   NOT NULL,
    id_comision   INT,
    CONSTRAINT ck_venta_estado CHECK (estado IN ('pendiente', 'confirmada', 'paga')),
    CONSTRAINT fk_venta_comprador FOREIGN KEY (doc_comprador) REFERENCES usuario_general(documento),
    CONSTRAINT fk_venta_comision  FOREIGN KEY (id_comision)   REFERENCES comision(id_comision)
    -- Máximo 5 entradas por venta: trigger en 02
);

-- ---------- Entradas ----------
CREATE TABLE entrada (
    nro_entrada     INT          AUTO_INCREMENT PRIMARY KEY,
    nro_venta       INT          NOT NULL,
    id_evento       INT          NOT NULL,
    nombre_estadio  VARCHAR(120) NOT NULL,
    nombre_sector   VARCHAR(80)  NOT NULL,
    fila            VARCHAR(10),
    asiento         VARCHAR(10),
    doc_propietario VARCHAR(30)  NOT NULL,  -- propietario actual
    codigo_qr       VARCHAR(255),
    id_dispositivo  INT,
    hora_validacion DATETIME(6),
    -- No vender dos veces el mismo asiento numerado en un evento/sector.
    -- En MySQL los NULL son distintos en un UNIQUE, así que las entradas sin
    -- fila/asiento (NULL) no chocan entre sí: equivale al índice parcial original.
    CONSTRAINT uq_entrada_asiento UNIQUE (id_evento, nombre_estadio, nombre_sector, fila, asiento),
    CONSTRAINT fk_entrada_venta       FOREIGN KEY (nro_venta)       REFERENCES venta(nro_venta),
    CONSTRAINT fk_entrada_evento      FOREIGN KEY (id_evento)       REFERENCES evento(id_evento),
    CONSTRAINT fk_entrada_sector      FOREIGN KEY (nombre_estadio, nombre_sector)
        REFERENCES sector(nombre_estadio, nombre),
    CONSTRAINT fk_entrada_propietario FOREIGN KEY (doc_propietario) REFERENCES usuario_general(documento),
    CONSTRAINT fk_entrada_dispositivo FOREIGN KEY (id_dispositivo)  REFERENCES dispositivo(id_dispositivo)
    -- La entrada solo puede ser de un sector habilitado para el evento: trigger en 02
);

-- ---------- Códigos QR / token dinámico (histórico de cadena de custodia) ----------
-- Solo un código activo por entrada: como MySQL no tiene índices parciales,
-- se usa una columna generada que vale nro_entrada cuando activo y NULL si no.
CREATE TABLE codigo_qr (
    id_codigo          INT          AUTO_INCREMENT PRIMARY KEY,
    nro_entrada        INT          NOT NULL,
    codigo             VARCHAR(255) NOT NULL,
    generado_en        DATETIME(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    activo             BOOLEAN      NOT NULL DEFAULT TRUE,
    nro_entrada_activo INT GENERATED ALWAYS AS (IF(activo, nro_entrada, NULL)) VIRTUAL,
    CONSTRAINT uq_codigo                 UNIQUE (codigo),
    CONSTRAINT uq_codigo_activo_entrada  UNIQUE (nro_entrada_activo),
    CONSTRAINT fk_codigo_entrada FOREIGN KEY (nro_entrada)
        REFERENCES entrada(nro_entrada) ON DELETE CASCADE
);

-- ---------- FuncionarioDispositivo (N:M) ----------
CREATE TABLE funcionario_dispositivo (
    doc_funcionario VARCHAR(30) NOT NULL,
    id_dispositivo  INT         NOT NULL,
    PRIMARY KEY (doc_funcionario, id_dispositivo),
    CONSTRAINT fk_fd_func FOREIGN KEY (doc_funcionario) REFERENCES funcionario(documento),
    CONSTRAINT fk_fd_disp FOREIGN KEY (id_dispositivo)  REFERENCES dispositivo(id_dispositivo)
);

-- ---------- Validación / ingreso (registra funcionario, código y dispositivo) ----------
CREATE TABLE validacion (
    nro_entrada     INT          NOT NULL,
    fecha_hora      DATETIME(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    id_codigo       INT          PRIMARY KEY,
    doc_funcionario VARCHAR(30)  NOT NULL,
    id_dispositivo  INT          NOT NULL,
    CONSTRAINT uq_validacion_entrada UNIQUE (nro_entrada),
    CONSTRAINT fk_val_entrada FOREIGN KEY (nro_entrada)     REFERENCES entrada(nro_entrada),
    CONSTRAINT fk_val_codigo  FOREIGN KEY (id_codigo)       REFERENCES codigo_qr(id_codigo),
    CONSTRAINT fk_val_func    FOREIGN KEY (doc_funcionario) REFERENCES funcionario(documento),
    CONSTRAINT fk_val_disp    FOREIGN KEY (id_dispositivo)  REFERENCES dispositivo(id_dispositivo)
);

-- ---------- Transferencias (log histórico; cadena de custodia) ----------
CREATE TABLE transferencia (
    nro_entrada  INT          NOT NULL,
    fecha_hora   DATETIME(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    contador     INT          NOT NULL DEFAULT 1 CHECK (contador BETWEEN 1 AND 3),
    doc_emisor   VARCHAR(30)  NOT NULL,
    doc_receptor VARCHAR(30)  NOT NULL,
    estado       VARCHAR(15)  NOT NULL DEFAULT 'pendiente',
    PRIMARY KEY (nro_entrada, fecha_hora),
    CONSTRAINT ck_transf_estado    CHECK (estado IN ('pendiente', 'aceptada', 'rechazada', 'cancelada')),
    CONSTRAINT ck_transf_distintos CHECK (doc_emisor <> doc_receptor),
    CONSTRAINT fk_transf_entrada  FOREIGN KEY (nro_entrada)  REFERENCES entrada(nro_entrada),
    CONSTRAINT fk_transf_emisor   FOREIGN KEY (doc_emisor)   REFERENCES usuario_general(documento),
    CONSTRAINT fk_transf_receptor FOREIGN KEY (doc_receptor) REFERENCES usuario_general(documento)
    -- Máximo 3 transferencias y no transferir validadas: trigger en 02
);

-- ---------- UsuarioTieneEntradas (tenencia actual; se mantiene por trigger) ----------
CREATE TABLE usuario_tiene_entradas (
    documento_usuario VARCHAR(30) NOT NULL,
    nro_entrada       INT         NOT NULL,
    PRIMARY KEY (documento_usuario, nro_entrada),
    CONSTRAINT fk_ute_usuario FOREIGN KEY (documento_usuario) REFERENCES usuario_general(documento),
    CONSTRAINT fk_ute_entrada FOREIGN KEY (nro_entrada)       REFERENCES entrada(nro_entrada)
);

-- ---------- FuncionarioAsignado ----------
CREATE TABLE funcionario_asignado (
    doc_funcionario VARCHAR(30)  NOT NULL,
    nombre_estadio  VARCHAR(120) NOT NULL,
    nombre_sector   VARCHAR(80)  NOT NULL,
    id_evento       INT          NOT NULL,
    PRIMARY KEY (doc_funcionario, nombre_estadio, nombre_sector, id_evento),
    CONSTRAINT fk_fa_func   FOREIGN KEY (doc_funcionario) REFERENCES funcionario(documento),
    CONSTRAINT fk_fa_evento FOREIGN KEY (id_evento)       REFERENCES evento(id_evento),
    CONSTRAINT fk_fa_sector FOREIGN KEY (nombre_estadio, nombre_sector)
        REFERENCES sector(nombre_estadio, nombre)
);

-- ---------- Índices de apoyo ----------
-- En MySQL/InnoDB cada columna de FK ya recibe un índice automático, así que
-- los índices de apoyo del modelo original (por evento, venta, comprador,
-- propietario, receptor, etc.) quedan cubiertos sin declararlos explícitamente.
