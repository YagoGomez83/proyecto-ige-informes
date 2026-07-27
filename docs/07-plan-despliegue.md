# 07 · Plan de Despliegue (On-Premise)

> **Supuestos de partida** (a confirmar con el área de infraestructura de la
> institución antes de la puesta en producción — marcados con 🔶): se asume
> un servidor Linux (Ubuntu Server 22.04/24.04 LTS o similar) con Docker
> instalado, sin política de backup institucional previa sobre la que
> apoyarse, y sin balanceador de carga externo.

## Topología

```
┌─────────────────────────────────────────────────────────┐
│                  Servidor institucional                  │
│                                                            │
│  ┌──────────────┐   ┌──────────────┐   ┌───────────────┐ │
│  │ Reverse Proxy│──▶│  IGE.Web     │──▶│  PostgreSQL   │ │
│  │ (Caddy/Nginx)│   │ (Blazor      │   │  (contenedor) │ │
│  │  TLS interno │   │  Server)     │   └───────────────┘ │
│  └──────────────┘   └──────┬───────┘                     │
│         ▲                  │           ┌───────────────┐ │
│         │                  └──────────▶│  MinIO        │ │
│    LAN institucional                   │ (contenedor)  │ │
│    (usuarios 10-30)                    └───────────────┘ │
└─────────────────────────────────────────────────────────┘
                    │
                    ▼
         Backup diario (fuera del servidor,
         ver sección Backups)
```

- Todos los servicios corren como contenedores Docker orquestados por
  **Docker Compose** (ver ADR-003).
- El servidor **no está expuesto a Internet**: solo accesible desde la LAN
  institucional o vía VPN si hay usuarios remotos. 🔶 *A confirmar: ¿algún
  analista necesita acceso fuera de la red institucional?*
- TLS terminado en el reverse proxy con certificado propio (interno) o
  emitido por una CA institucional si existe. Nunca HTTP plano, ni siquiera
  dentro de la LAN.

## Servicios del `docker-compose.yml`

| Servicio | Imagen base | Puertos expuestos | Notas |
|---|---|---|---|
| `reverse-proxy` | Caddy (config automática de TLS) o Nginx | 443 (LAN) | Único punto de entrada |
| `web` | `mcr.microsoft.com/dotnet/aspnet` (self-contained) | interno 8080 | Blazor Server + healthcheck en `/health` |
| `worker` | Misma imagen que `web`, distinto entrypoint | — | Procesa extracción de PDFs y jobs en background (Hangfire) |
| `postgres` | `postgres:16` | interno 5432 | Volumen persistente `pgdata` |
| `minio` | `minio/minio` | interno 9000/9001 | Volumen persistente `miniodata`, bucket privado `ige-informes` |

Todos los contenedores corren con **usuario no-root** y `restart: unless-stopped`.

## Ambientes

Dado el contexto (equipo de desarrollo unipersonal + institución pequeña),
se recomienda:

- **Producción**: el servidor institucional descripto arriba.
- **Desarrollo/pruebas**: el mismo `docker-compose.yml` corriendo en la
  notebook del desarrollador, con datos de prueba (nunca datos reales de
  causas/personas).
- No se arma un ambiente de "staging" separado en esta v1 — sería
  sobre-ingeniería para el tamaño del equipo; si el proyecto crece, se
  puede sumar más adelante sin rediseñar nada.

## Backups

| Elemento | Método | Frecuencia | Retención |
|---|---|---|---|
| Base de datos PostgreSQL | `pg_dump` automatizado (cron o contenedor sidecar) | Diario | 30 días rotando + 1 backup mensual de largo plazo |
| Bucket MinIO (PDFs, imágenes) | `mc mirror` o snapshot del volumen Docker | Diario | Igual que arriba |
| Configuración (`docker-compose.yml`, `.env` sin secretos reales) | Control de versiones (repo privado) | En cada cambio | — |

- Los backups se copian **fuera del servidor** (NAS institucional, otro
  servidor, o almacenamiento externo) — un backup que vive en el mismo disco
  que falla no sirve. 🔶 *A confirmar: destino disponible para backups
  externos.*
- **Prueba de restauración**: se ejecuta un simulacro de restauración
  completa (DB + MinIO) al menos una vez antes de ir a producción, y luego
  trimestralmente. Un backup nunca probado es un backup que no existe.

### Implementación (Fase 5)

- Scripts en `docker/backup/scripts/`: `backup-postgres.sh` (`pg_dump -Fc`,
  con rotación por `BACKUP_RETENTION_DIAS`), `backup-minio.sh` (`mc mirror`
  incremental), `restore-postgres.sh` (dropea y recrea la base indicada,
  pensado para el simulacro o para recuperación real ante desastre).
- Sidecar `docker/backup/Dockerfile` (imagen basada en `postgres:16`, trae
  `pg_dump`/`pg_restore` nativos + `mc` + `cron`), levantado como servicio
  opcional vía `docker-compose.backup.yml`:
  ```
  docker compose -f docker-compose.yml -f docker-compose.backup.yml up -d
  ```
  No se fusionó al `docker-compose.yml` principal para no forzar este
  volumen/imagen extra en un entorno de desarrollo que no lo necesita.
- Cron programado a las 03:00 (`docker/backup/crontab`). Backup manual (sin
  esperar al cron): `docker exec <contenedor-backup> /scripts/entrypoint-job.sh`.
- El volumen `backups` del compose es local — sirve para el simulacro y
  desarrollo, pero en producción **debe** montarse sobre un disco/NAS
  externo al servidor antes de confiar en él (ver 🔶 arriba).
- **Simulacro de restauración ejecutado y verificado** (2026-07-27, entorno
  de desarrollo Docker local): `pg_dump` → `pg_restore` contra una base de
  prueba (`ige_informes_restore_test`, descartada después) reprodujo
  exactamente los mismos conteos de filas que el origen (854 Cámaras, 80
  Dependencias, 54 Localidades, 1 Informe); los 5 archivos de MinIO
  espejados verificaron tamaño no-cero e íntegro. Pendiente repetir el
  mismo simulacro contra el servidor de producción real (con el volumen de
  backups ya montado sobre almacenamiento externo) antes del go-live.

## Monitoreo y logs

- Logs estructurados (Serilog) de la aplicación, con nivel `Warning`+ 
  enviado también a archivo rotado en disco (no solo stdout del contenedor).
- Healthcheck HTTP (`/health`) en `web`, verificado por Docker Compose
  (`healthcheck:`) y opcionalmente por un cron simple que alerte por email/
  Telegram si el servicio cae.
- El `AuditLog` (accesos y cambios) es la fuente para cualquier auditoría
  posterior — no reemplaza, pero complementa, los logs técnicos.

## Proceso de actualización (deploy)

1. Se buildea la nueva imagen localmente o en CI.
2. El Administrador ejecuta el script de deploy (`docker compose pull &&
   docker compose up -d`) en una ventana de mantenimiento acordada (fuera
   de horario de uso intensivo del equipo de analítica).
3. Las migraciones de EF Core corren automáticamente al iniciar `web`
   (o vía comando explícito `dotnet ef database update`, a decidir en
   `IGE.Informes.Infrastructure`).
4. Se verifica el healthcheck y se revisan los logs de arranque antes de
   dar por cerrado el deploy.
5. No hay despliegue automático sin intervención humana — es infraestructura
   crítica de un organismo público (ver `06-seguridad-amenazas.md`).

## Dimensionamiento estimado

Para 10-30 usuarios concurrentes, 500-5000 informes históricos + crecimiento
anual, y Casos de Análisis nuevos desde cero:

| Recurso | Estimación inicial | Notas |
|---|---|---|
| CPU | 2-4 vCPU | Blazor Server + worker de extracción son livianos en reposo |
| RAM | 8 GB | PostgreSQL + MinIO + app .NET |
| Disco | 100-200 GB | Depende del tamaño acumulado de imágenes/PDFs — las imágenes de evidencia son el principal consumidor |

🔶 *Esta estimación es conservadora y debe validarse con el equipo de
infraestructura antes de aprovisionar el servidor definitivo.*

## Plan de recuperación ante desastres (resumen)

1. Si el servidor completo se pierde: reinstalar Docker, restaurar el
   backup más reciente de PostgreSQL y MinIO, restaurar `docker-compose.yml`
   desde el repositorio, `docker compose up -d`.
2. Objetivo de tiempo de recuperación (RTO) sugerido: **4 horas hábiles**.
3. Objetivo de punto de recuperación (RPO) sugerido: **24 horas** (backup
   diario) — si se necesita un RPO menor, se debe evaluar replicación
   continua de PostgreSQL, lo cual es sobre-ingeniería para el volumen
   actual salvo que la institución lo exija explícitamente.
