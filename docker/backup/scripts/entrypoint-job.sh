#!/bin/sh
set -eu

# Job diario ejecutado por cron: carga las variables de entorno del
# contenedor (cron no las hereda) y corre ambos backups en secuencia.
# Si backup-postgres falla, no se intenta backup-minio — mejor un backup
# faltante y detectable que uno parcial silencioso.

. /etc/ige-backup.env

echo "=== $(date -Iseconds) Iniciando backup diario ==="
/scripts/backup-postgres.sh
/scripts/backup-minio.sh
echo "=== $(date -Iseconds) Backup diario completado ==="
