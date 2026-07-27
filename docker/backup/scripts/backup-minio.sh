#!/bin/sh
set -eu

# Backup diario del bucket de MinIO (PDFs, imágenes de Evidencia) vía
# `mc mirror` incremental hacia un directorio local montado como volumen
# persistente distinto del de MinIO. Pensado para correr como cron dentro
# del contenedor sidecar "backup" (ver docker-compose.backup.yml).
#
# Variables de entorno esperadas:
#   MINIO_ENDPOINT (ej. http://minio:9000), MINIO_ROOT_USER, MINIO_ROOT_PASSWORD
#   MINIO_BUCKET (ej. ige-informes)
# BACKUP_DIR: directorio destino del snapshot.

MINIO_ENDPOINT="${MINIO_ENDPOINT:-http://minio:9000}"
MINIO_BUCKET="${MINIO_BUCKET:-ige-informes}"
BACKUP_DIR="${BACKUP_DIR:-/backups/minio}"

mkdir -p "$BACKUP_DIR"

mc alias set backup-source "$MINIO_ENDPOINT" "$MINIO_ROOT_USER" "$MINIO_ROOT_PASSWORD" > /dev/null

echo "[backup-minio] Espejando bucket '${MINIO_BUCKET}' -> ${BACKUP_DIR}"
mc mirror --overwrite "backup-source/${MINIO_BUCKET}" "$BACKUP_DIR"

echo "[backup-minio] OK"
