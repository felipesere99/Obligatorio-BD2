# Obligatorio BD2

Sistema de ticketing.

## Arquitectura de la base

La lógica vive en **triggers** (reglas duras en MySQL) y en **C#** (validaciones
y orquestación en el server). No hay stored procedures ni functions: el usuario
de la UCU (`xr_g4_admin`) solo tiene `SELECT`, `INSERT`, `UPDATE`, `DELETE`,
`CREATE`, `DROP`, `REFERENCES`, `INDEX`, `ALTER` y `TRIGGER` — sin
`CREATE ROUTINE`, `ALTER ROUTINE` ni `EXECUTE`.

Scripts en `db/init/` (mismo esquema local y UCU):

| Archivo | Contenido |
|---------|-----------|
| `01_schema.sql` | Tablas, constraints e índices |
| `02_triggers.sql` | Triggers de negocio (`SIGNAL 45000` → HTTP 400) |
| `04_seed.sql` | Datos de prueba |

Los endpoints usan SQL directo y transacciones en C#; los triggers siguen
validando capacidad, sectores habilitados, máximo de entradas, etc.

## Requisitos

- [Docker](https://docs.docker.com/get-docker/) con Docker Compose
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Levantar la base de datos

La base local es **MySQL 8** (vía Docker).

```bash
docker compose up -d
```

Espera a que el healthcheck pase (verde) antes de arrancar el server:

```bash
docker compose ps   # Status debe ser "healthy"
```

## Levantar el server

```bash
dotnet run --project server
```

El server escucha en `http://localhost:5050` por defecto.

## Ejecutar el cliente

En otra terminal:

```bash
dotnet run --project client
```

Debe imprimir:

```
db: pong
```

El cliente ahora es interactivo: pide un documento para iniciar sesión y
muestra un menú según el rol.

## Verificar /health directamente

```bash
curl http://localhost:5050/health
# {"db":"pong"}
```

## Variables locales (`.env`)

Copiá la plantilla y completá tus valores (sobre todo la password de la UCU):

```bash
cp .env.example .env
```

El `.env` está en `.gitignore` (no se sube). Lo usan `docker compose` (credenciales
de la base MySQL local) y `scripts/run-ucu.sh` (toma `UCU_DB_PASSWORD` de ahí en
vez de pedirla por consola).

## Elegir contra qué base corre el server

El server lee `DB_CONNECTION_STRING`; si no está, usa la base local de
`appsettings.Development.json`. Hay dos scripts para no tener que acordarse de
la cadena (se elige una base por arranque, no las dos a la vez):

```bash
./scripts/run-local.sh   # base LOCAL (Docker MySQL, localhost:3306)
./scripts/run-ucu.sh     # base UCU (Grupo 4); pide la contraseña
```

`run-ucu.sh` no guarda la contraseña en el repo: la pide por consola o la toma
de `$UCU_DB_PASSWORD`. Acepta overrides (`UCU_DB_HOST`, `UCU_DB_PORT`,
`UCU_DB_NAME`, `UCU_DB_USER`) con defaults para el Grupo 4.

## Base compartida de la UCU (MySQL)

Si preferís hacerlo a mano, apuntá el server con `DB_CONNECTION_STRING`:

```bash
export DB_CONNECTION_STRING="Server=mysql.reto-ucu.net;Port=50006;Database=XR_Grupo4;User ID=xr_g4_admin;Password=TU_PASSWORD"
dotnet run --project server
```

> La contraseña se cambia en el primer login; no la pongas en el repo (va en el
> `.env`, que está en `.gitignore`). Para aplicar `db/init/` en la UCU, ejecutá
> `01_schema.sql`, `02_triggers.sql` y `04_seed.sql` en ese orden (DataGrip o
> cliente `mysql`; los triggers usan `DELIMITER`). No hace falta `CREATE ROUTINE`.

## Login y datos de prueba

El esquema y el seed (`db/init/`) se cargan en la **primera** inicialización del
volumen. Si cambiás los scripts de `db/init`, recargá con:

```bash
docker compose down -v && docker compose up -d
```

> El volumen ahora es `mysqldata` (MySQL). Los scripts de `db/init/` corren
> en orden alfabético en la **primera** inicialización.

Documentos de prueba (login por documento, sin password):

| Documento | Rol               | Nombre |
|-----------|-------------------|--------|
| `ADM-1`   | administrador     | Admin  |
| `FUN-1`   | funcionario       | Fabián |
| `UG-1`    | usuario_general   | Ana (tiene 2 entradas) |
| `UG-2`    | usuario_general   | Beto   |
| `UG-3`    | usuario_general   | Caro   |

```bash
curl -X POST http://localhost:5050/login \
  -H 'Content-Type: application/json' -d '{"documento":"UG-1"}'
# {"documento":"UG-1","rol":"usuario_general","nombre":"Ana"}
```

Las peticiones autenticadas reenvían la sesión en los headers
`X-Documento` y `X-Rol` (lo hace el `ApiClient` automáticamente).

## Variables de entorno

| Variable              | Descripción                              | Default                                    |
|-----------------------|------------------------------------------|--------------------------------------------|
| `DB_CONNECTION_STRING`| Override de connection string completa   | Valor de `appsettings.Development.json`    |
| `SERVER_URL`          | URL base del server (solo para client)   | `http://localhost:5050`                    |
