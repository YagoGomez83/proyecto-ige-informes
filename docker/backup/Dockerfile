# Sidecar de backup: pg_dump/pg_restore (nativos de la imagen postgres)
# + mc (MinIO client) + cron, para correr backup-postgres.sh y
# backup-minio.sh diariamente sin depender de un cron del host.
FROM postgres:16

RUN apt-get update \
    && apt-get install -y --no-install-recommends cron curl ca-certificates \
    && curl -fsSL https://dl.min.io/client/mc/release/linux-amd64/mc -o /usr/local/bin/mc \
    && chmod +x /usr/local/bin/mc \
    && rm -rf /var/lib/apt/lists/*

COPY scripts/ /scripts/
RUN chmod +x /scripts/*.sh

COPY crontab /etc/cron.d/ige-backup
RUN chmod 0644 /etc/cron.d/ige-backup && crontab /etc/cron.d/ige-backup

# cron no hereda el entorno del contenedor por defecto — se vuelca a un
# archivo que los scripts pueden sourcear, y el propio entrypoint lo
# regenera en cada arranque por si las variables cambiaron.
ENTRYPOINT ["/scripts/entrypoint.sh"]
