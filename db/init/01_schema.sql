-- ============================================================
--  01_schema.sql — Esquema relacional (Ticketing — Mundial 2026)
--  Basado en el Pasaje a Tablas final del informe.
--  Las líneas marcadas con  -- [+]  son agregados mínimos para
--  poder cumplir la letra (no estaban explícitos en el pasaje).
-- ============================================================

-- ---------- Dispositivos de validación ----------
CREATE TABLE dispositivo (
    id_dispositivo SERIAL PRIMARY KEY
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
    fecha             DATE         NOT NULL DEFAULT CURRENT_DATE,
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
    fecha_alta          DATE         NOT NULL DEFAULT CURRENT_DATE,
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
    nombre_estadio VARCHAR(120)  NOT NULL REFERENCES estadio(nombre) ON DELETE CASCADE,
    nombre         VARCHAR(80)   NOT NULL,
    capacidad      INT           NOT NULL CHECK (capacidad > 0),
    costo_entrada  NUMERIC(12,2) NOT NULL CHECK (costo_entrada >= 0),
    PRIMARY KEY (nombre_estadio, nombre)
);

-- ---------- Eventos ----------
CREATE TABLE evento (
    id_evento       SERIAL       PRIMARY KEY,
    nombre          VARCHAR(150) NOT NULL,
    fecha_inicio    TIMESTAMPTZ  NOT NULL,
    fecha_fin       TIMESTAMPTZ  NOT NULL,
    pais_local      VARCHAR(60)  REFERENCES equipo(pais),
    pais_visitante  VARCHAR(60)  REFERENCES equipo(pais),
    nombre_estadio  VARCHAR(120) NOT NULL REFERENCES estadio(nombre),
    CONSTRAINT ck_evento_fechas  CHECK (fecha_fin > fecha_inicio),
    CONSTRAINT ck_evento_equipos CHECK (pais_local IS DISTINCT FROM pais_visitante)
    -- No superposición de eventos en un mismo estadio: trigger en 02
);

-- ---------- Sectores habilitados por evento ----------
CREATE TABLE evento_sector (
    id_evento      INT          NOT NULL REFERENCES evento(id_evento) ON DELETE CASCADE,
    nombre_estadio VARCHAR(120) NOT NULL,
    nombre_sector  VARCHAR(80)  NOT NULL,
    PRIMARY KEY (id_evento, nombre_estadio, nombre_sector),
    FOREIGN KEY (nombre_estadio, nombre_sector) REFERENCES sector(nombre_estadio, nombre)
);

-- ---------- Comisión variable en el tiempo ----------
CREATE TABLE comision (
    id_comision   SERIAL       PRIMARY KEY,
    porcentaje    NUMERIC(5,2) NOT NULL CHECK (porcentaje >= 0),
    vigente_desde TIMESTAMPTZ  NOT NULL,
    vigente_hasta TIMESTAMPTZ,
    CONSTRAINT ck_comision_rango CHECK (vigente_hasta IS NULL OR vigente_hasta > vigente_desde)
);

-- ---------- Ventas ----------
CREATE TABLE venta (
    nro_venta     SERIAL        PRIMARY KEY,
    monto_total   NUMERIC(14,2) NOT NULL CHECK (monto_total >= 0),
    estado        VARCHAR(15)   NOT NULL DEFAULT 'pendiente',
    fecha         TIMESTAMPTZ   NOT NULL DEFAULT now(),
    doc_comprador VARCHAR(30)   NOT NULL REFERENCES usuario_general(documento),
    id_comision   INT           REFERENCES comision(id_comision),
    CONSTRAINT ck_venta_estado CHECK (estado IN ('pendiente', 'confirmada', 'paga'))
    -- Máximo 5 entradas por venta: trigger en 02
);

-- ---------- Entradas ----------
CREATE TABLE entrada (
    nro_entrada     SERIAL       PRIMARY KEY,
    nro_venta       INT          NOT NULL REFERENCES venta(nro_venta),
    id_evento       INT          NOT NULL REFERENCES evento(id_evento),
    nombre_estadio  VARCHAR(120) NOT NULL,
    nombre_sector   VARCHAR(80)  NOT NULL,
    fila            VARCHAR(10),
    asiento         VARCHAR(10),
    doc_propietario VARCHAR(30)  NOT NULL REFERENCES usuario_general(documento),  -- propietario actual
    codigo_qr       VARCHAR(255),
    id_dispositivo  INT          REFERENCES dispositivo(id_dispositivo),
    hora_validacion TIMESTAMPTZ,
    FOREIGN KEY (nombre_estadio, nombre_sector) REFERENCES sector(nombre_estadio, nombre)
    -- La entrada solo puede ser de un sector habilitado para el evento: trigger en 02
);

-- No vender dos veces el mismo asiento numerado en un evento/sector
CREATE UNIQUE INDEX uq_entrada_asiento
    ON entrada (id_evento, nombre_estadio, nombre_sector, fila, asiento)
    WHERE fila IS NOT NULL AND asiento IS NOT NULL;

-- ---------- Códigos QR / token dinámico (histórico de cadena de custodia) ----------
CREATE TABLE codigo_qr (
    id_codigo   SERIAL       PRIMARY KEY,
    nro_entrada INT          NOT NULL REFERENCES entrada(nro_entrada) ON DELETE CASCADE,
    codigo      VARCHAR(255) NOT NULL,
    generado_en TIMESTAMPTZ  NOT NULL DEFAULT now(),
    activo      BOOLEAN      NOT NULL DEFAULT TRUE,
    CONSTRAINT uq_codigo UNIQUE (codigo)
);
CREATE UNIQUE INDEX uq_codigo_activo_por_entrada ON codigo_qr (nro_entrada) WHERE activo;

-- ---------- FuncionarioDispositivo (N:M) ----------
CREATE TABLE funcionario_dispositivo (
    doc_funcionario VARCHAR(30) NOT NULL REFERENCES funcionario(documento),
    id_dispositivo  INT         NOT NULL REFERENCES dispositivo(id_dispositivo),
    PRIMARY KEY (doc_funcionario, id_dispositivo)
);

-- ---------- Validación / ingreso (registra funcionario, código y dispositivo) ----------
CREATE TABLE validacion (
    nro_entrada     INT          NOT NULL REFERENCES entrada(nro_entrada),
    fecha_hora      TIMESTAMPTZ  NOT NULL DEFAULT now(),
    id_codigo       INT          PRIMARY KEY REFERENCES codigo_qr(id_codigo),
    doc_funcionario VARCHAR(30)  NOT NULL REFERENCES funcionario(documento),
    id_dispositivo  INT          NOT NULL REFERENCES dispositivo(id_dispositivo),
    CONSTRAINT uq_validacion_entrada UNIQUE (nro_entrada)
);

-- ---------- Transferencias (log histórico; cadena de custodia) ----------
CREATE TABLE transferencia (
    nro_entrada  INT          NOT NULL REFERENCES entrada(nro_entrada),
    fecha_hora   TIMESTAMPTZ  NOT NULL DEFAULT now(),
    contador     INT          NOT NULL DEFAULT 1 CHECK (contador BETWEEN 1 AND 3),
    doc_emisor   VARCHAR(30)  NOT NULL REFERENCES usuario_general(documento),
    doc_receptor VARCHAR(30)  NOT NULL REFERENCES usuario_general(documento),
    estado       VARCHAR(15)  NOT NULL DEFAULT 'pendiente',
    PRIMARY KEY (nro_entrada, fecha_hora),
    CONSTRAINT ck_transf_estado    CHECK (estado IN ('pendiente', 'aceptada', 'rechazada', 'cancelada')),
    CONSTRAINT ck_transf_distintos CHECK (doc_emisor <> doc_receptor)
    -- Máximo 3 transferencias y no transferir validadas: trigger en 02
);

-- ---------- UsuarioTieneEntradas (tenencia actual; se mantiene por trigger) ----------
CREATE TABLE usuario_tiene_entradas (
    documento_usuario VARCHAR(30) NOT NULL REFERENCES usuario_general(documento),
    nro_entrada       INT         NOT NULL REFERENCES entrada(nro_entrada),
    PRIMARY KEY (documento_usuario, nro_entrada)
);

-- ---------- FuncionarioAsignado ----------
CREATE TABLE funcionario_asignado (
    doc_funcionario VARCHAR(30)  NOT NULL REFERENCES funcionario(documento),
    nombre_estadio  VARCHAR(120) NOT NULL,
    nombre_sector   VARCHAR(80)  NOT NULL,
    id_evento       INT          NOT NULL REFERENCES evento(id_evento),
    PRIMARY KEY (doc_funcionario, nombre_estadio, nombre_sector, id_evento),
    FOREIGN KEY (nombre_estadio, nombre_sector) REFERENCES sector(nombre_estadio, nombre)
);

-- ---------- Índices de apoyo ----------
CREATE INDEX ix_entrada_evento      ON entrada (id_evento);
CREATE INDEX ix_entrada_propietario ON entrada (doc_propietario);
CREATE INDEX ix_entrada_venta       ON entrada (nro_venta);
CREATE INDEX ix_venta_comprador     ON venta (doc_comprador);
CREATE INDEX ix_codigo_entrada      ON codigo_qr (nro_entrada);
CREATE INDEX ix_transf_entrada      ON transferencia (nro_entrada);
CREATE INDEX ix_transf_receptor     ON transferencia (doc_receptor);
CREATE INDEX ix_asignacion_evento   ON funcionario_asignado (id_evento);
