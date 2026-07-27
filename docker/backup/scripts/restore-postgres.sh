#!/bin/sh
set -eu

# Restaura un dump de Postgres generado por backup-postgres.sh. Uso:
#   ./restore-postgres.sh /backups/postgres/ige_informes_20260101-030000.dump
#
# ADVERTENCIA: dropea y recrea la base de datos indicada en PGDATABASE
# antes de restaurar — pensado para un simulacro de restauración contra
# una base de prueba (docs/07-plan-despliegue.md), o para una recuperación
# real ante desastre. No correr contra la base de producción sin confirmar
# explícitamente el nombre de PGDATABASE primero.

if [ $# -ne 1 ]; then
    echo "Uso: $0 <ruta-al-archivo.dump>" >&2
    exit 1
fi

ARCHIVO="$1"

if [ ! -f "$ARCHIVO" ]; then
    echo "[restore-postgres] ERROR: no existe el archivo '${ARCHIVO}'" >&2
    exit 1
fi

echo "[restore-postgres] Restaurando '${ARCHIVO}' en la base '${PGDATABASE}' (host ${PGHOST})"
echo "[restore-postgres] Esto DROPEA los datos actuales de '${PGDATABASE}'. Ctrl+C en 5s para cancelar."
sleep 5

dropdb --if-exists "$PGDATABASE"
createdb "$PGDATABASE"
pg_restore --no-owner --no-privileges -d "$PGDATABASE" "$ARCHIVO"

echo "[restore-postgres] OK — verificar manualmente el conteo de filas contra el backup de origen"
