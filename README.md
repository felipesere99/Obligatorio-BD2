# Obligatorio BD2

Sistema de ticketing — esqueleto inicial.

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

> La contraseña inicial (`BD2Obligatorio2026`) se cambia en el primer login;
> usá la nueva en la cadena de conexión. Los scripts de `db/init/` se pueden
> aplicar a esa base con DataGrip (soporta `DELIMITER`) o con el cliente
> `mysql`.

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
