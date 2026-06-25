-- ============================================================
--  04_seed.sql — Datos de prueba (dialecto MySQL 8).
--  (Solo corre en la primera inicialización del volumen; para
--   recargar: `docker compose down -v && docker compose up -d`.)
-- ============================================================

-- ---------- Equipos ----------
INSERT INTO equipo(pais, nombre) VALUES
 ('URU','Uruguay'), ('ARG','Argentina'), ('BRA','Brasil'), ('ESP','España');

-- ---------- Estadio + sectores ----------
INSERT INTO estadio(nombre, direccion) VALUES
 ('Centenario','Av. Ricaldoni s/n, Montevideo');
INSERT INTO sector(nombre_estadio, nombre, capacidad, costo_entrada) VALUES
 ('Centenario','A', 100, 150.00),
 ('Centenario','B',  80, 100.00);

-- ---------- Evento + sectores habilitados ----------
-- Insertamos id_evento explícito; MySQL continúa el AUTO_INCREMENT desde el máximo.
INSERT INTO evento(id_evento, nombre, fecha_inicio, fecha_fin, pais_local, pais_visitante, nombre_estadio) VALUES
 (1,'Uruguay vs Argentina','2026-06-15 18:00:00','2026-06-15 20:00:00','URU','ARG','Centenario');
INSERT INTO evento_sector(id_evento, nombre_estadio, nombre_sector) VALUES
 (1,'Centenario','A'),
 (1,'Centenario','B');

-- ---------- Comisión vigente (5%) ----------
INSERT INTO comision(porcentaje, vigente_desde) VALUES (5.00, '2026-01-01 00:00:00');

-- ---------- Administrador ----------
INSERT INTO administrador(documento, nombre, apellido, correo, cargo) VALUES
 ('ADM-1','Admin','Sede','admin@ticketing.uy','Administrador País Sede');

-- ---------- Funcionario + dispositivo + asignación ----------
INSERT INTO funcionario(documento, nombre, apellido, correo, nro_legajo) VALUES
 ('FUN-1','Fabián','Validez','funcio@ticketing.uy','LEG-001');
INSERT INTO dispositivo(nro_serie, marca, modelo, habilitado) VALUES
 ('VAL-0001','Zebra','TC21', TRUE);   -- id_dispositivo = 1
INSERT INTO funcionario_dispositivo(doc_funcionario, id_dispositivo) VALUES
 ('FUN-1', 1);
INSERT INTO funcionario_asignado(doc_funcionario, nombre_estadio, nombre_sector, id_evento) VALUES
 ('FUN-1','Centenario','A',1),
 ('FUN-1','Centenario','B',1);

-- ---------- Usuarios generales ----------
INSERT INTO usuario_general(documento, nombre, apellido, correo, estado_verificacion) VALUES
 ('UG-1','Ana','Pérez','ana@mail.com',  TRUE),
 ('UG-2','Beto','Gómez','beto@mail.com',TRUE),
 ('UG-3','Caro','Díaz','caro@mail.com', FALSE);

INSERT INTO telefono_usuario(documento, telefono) VALUES
 ('UG-1','099111222'), ('UG-1','22001122'), ('UG-2','098333444');

-- ---------- Venta de ejemplo: UG-1 compra 2 entradas en sector A ----------
-- monto = 2 * 150 + 5% = 315.00
INSERT INTO venta(monto_total, estado, doc_comprador, id_comision) VALUES
 (315.00,'paga','UG-1',1);
INSERT INTO entrada(nro_venta, id_evento, nombre_estadio, nombre_sector, fila, asiento, doc_propietario) VALUES
 (1,1,'Centenario','A','1','1','UG-1'),
 (1,1,'Centenario','A','1','2','UG-1');

-- ---------- Código QR activo por entrada (para probar validación) ----------
INSERT INTO codigo_qr(nro_entrada, codigo) VALUES
 (1,'QR-ENTRADA-1-INIT'),
 (2,'QR-ENTRADA-2-INIT');

-- ---------- Credenciales demo (contraseña: "demo1234") ----------
-- Hashes BCrypt work-factor 11 generados con BCrypt.Net-Next 4.0.3.
-- Para regenerar: BCrypt.Net.BCrypt.HashPassword("demo1234", workFactor: 11)
INSERT INTO credencial(documento, hash) VALUES
 ('ADM-1', '$2a$11$357dpmKEroxarnRnJ7ot/OYJH92n9/oZMwID3bpl.zC9.65wfc7Z.'),
 ('FUN-1', '$2a$11$tggzvxj6X5vztN9Z2WL.6eMC0Ju1e51hTxeTtWIOIUpoIdnUMF0Yq'),
 ('UG-1',  '$2a$11$uD/MQP7ROzF5km//TtdgG.vidXqseBSGQMQS7Fz5CbhJPUccv.hsK'),
 ('UG-2',  '$2a$11$xc44KY1m.lENZHWcTqr4GO1hOnf01T./INTmJgineQeHd.6iGXe3W'),
 ('UG-3',  '$2a$11$4evChi/DxAgmRETTi0H7Uucn.5mDWq52xnR5I/jqTKxjlOYeeV3v6');
