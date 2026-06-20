#!/usr/bin/env bash
# Levanta el server contra la base MySQL compartida de la UCU (Grupo 4).
# La contraseña NO se guarda en el repo: se toma de $UCU_DB_PASSWORD o se pide.
#
# Uso:
#   ./scripts/run-ucu.sh
#   UCU_DB_PASSWORD='mi_pass' ./scripts/run-ucu.sh      # sin prompt
#
# Variables opcionales (con sus defaults para el Grupo 4):
#   UCU_DB_HOST=mysql.reto-ucu.net
#   UCU_DB_PORT=50006
#   UCU_DB_NAME=XR_Grupo4
#   UCU_DB_USER=xr_g4_admin
set -euo pipefail

cd "$(dirname "$0")/.."

# Cargar .env si existe (UCU_DB_PASSWORD, overrides, etc.) sin pisar lo que ya
# venga por entorno explícito.
if [ -f .env ]; then
  set -a
  # shellcheck disable=SC1091
  . ./.env
  set +a
fi

UCU_DB_HOST="${UCU_DB_HOST:-mysql.reto-ucu.net}"
UCU_DB_PORT="${UCU_DB_PORT:-50006}"
UCU_DB_NAME="${UCU_DB_NAME:-XR_Grupo4}"
UCU_DB_USER="${UCU_DB_USER:-xr_g4_admin}"

# Pedir la contraseña si no vino por entorno (no se imprime en pantalla).
if [ -z "${UCU_DB_PASSWORD:-}" ]; then
  read -r -s -p "Password de ${UCU_DB_USER}@${UCU_DB_HOST}: " UCU_DB_PASSWORD
  echo
fi

export DB_CONNECTION_STRING="Server=${UCU_DB_HOST};Port=${UCU_DB_PORT};Database=${UCU_DB_NAME};User ID=${UCU_DB_USER};Password=${UCU_DB_PASSWORD}"

echo ">> Base: UCU (${UCU_DB_USER}@${UCU_DB_HOST}:${UCU_DB_PORT}/${UCU_DB_NAME})"
exec dotnet run --project server
