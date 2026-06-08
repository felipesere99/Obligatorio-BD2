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

## Verificar /health directamente

```bash
curl http://localhost:5050/health
# {"db":"pong"}
```

## Variables de entorno

| Variable              | Descripción                              | Default                                    |
|-----------------------|------------------------------------------|--------------------------------------------|
| `DB_CONNECTION_STRING`| Override de connection string completa   | Valor de `appsettings.Development.json`    |
| `SERVER_URL`          | URL base del server (solo para client)   | `http://localhost:5050`                    |
