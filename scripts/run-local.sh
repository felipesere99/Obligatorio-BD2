#!/usr/bin/env bash
# Levanta el server contra la base MySQL local (Docker, localhost:3306).
# Uso: ./scripts/run-local.sh
set -euo pipefail

# Ubicarse en la raíz del repo sin importar desde dónde se invoque.
cd "$(dirname "$0")/.."

# Asegurar que NO haya un override apuntando a la UCU en este proceso.
unset DB_CONNECTION_STRING

export ASPNETCORE_ENVIRONMENT=Development

echo ">> Base: LOCAL (Docker MySQL en localhost:3306)"
echo ">> Asegurate de tener la base arriba:  docker compose up -d"
exec dotnet run --project server
