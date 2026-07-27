#!/bin/sh
set -eu

# Vuelca las variables de entorno actuales del contenedor a un archivo
# que el job de cron (que arranca en un entorno limpio) puede sourcear —
# cron no hereda automáticamente el entorno del proceso que lo lanzó.
env | grep -E '^(PG|MINIO_)' | sed 's/^/export /' > /etc/ige-backup.env

echo "[entrypoint] Backup sidecar listo. Cron programado (ver /etc/cron.d/ige-backup)."
echo "[entrypoint] Para forzar un backup manual: docker exec <contenedor> /scripts/entrypoint-job.sh"

exec cron -f
