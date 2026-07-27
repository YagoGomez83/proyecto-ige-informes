#!/bin/sh
set -eu

# Backup diario de PostgreSQL vía pg_dump, en formato custom (-Fc) para
# permitir restauración parcial/paralela con pg_restore. Pensado para
# correr como cron dentro del contenedor sidecar "backup" (ver
# docker-compose.backup.yml) — no se ejecuta en el host directamente.
#
# Variables de entorno esperadas (ya presentes en el compose principal):
#   PGHOST, PGPORT, PGUSER, PGPASSWORD, PGDATABASE
# BACKUP_DIR: directorio destino (volumen persistente, distinto del de
#   Postgres — un backup que vive en el mismo disco que falla no sirve,
#   ver docs/07-plan-despliegue.md).
# BACKUP_RETENTION_DIAS: cuántos backups diarios conservar (rotación).

BACKUP_DIR="${BACKUP_DIR:-/backups/postgres}"
BACKUP_RETENTION_DIAS="${BACKUP_RETENTION_DIAS:-30}"
TIMESTAMP=$(date +%Y%m%d-%H%M%S)
ARCHIVO="${BACKUP_DIR}/ige_informes_${TIMESTAMP}.dump"

mkdir -p "$BACKUP_DIR"

echo "[backup-postgres] Iniciando dump de '${PGDATABASE}' -> ${ARCHIVO}"
pg_dump -Fc --no-owner --no-privileges -f "$ARCHIVO"
echo "[backup-postgres] Dump completado ($(du -h "$ARCHIVO" | cut -f1))"

echo "[backup-postgres] Rotando backups con más de ${BACKUP_RETENTION_DIAS} días"
find "$BACKUP_DIR" -name 'ige_informes_*.dump' -mtime "+${BACKUP_RETENTION_DIAS}" -print -delete

echo "[backup-postgres] OK"
