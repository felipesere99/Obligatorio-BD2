# Obligatorio BD2

Sistema de ticketing — esqueleto inicial.

## Requisitos

- [Docker](https://docs.docker.com/get-docker/) con Docker Compose
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Levantar la base de datos

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

## Login y datos de prueba

El esquema y el seed (`db/init/`) se cargan en la **primera** inicialización del
volumen. Si cambiás los scripts de `db/init`, recargá con:

```bash
docker compose down -v && docker compose up -d
```

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
