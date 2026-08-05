# 07 · Plan de Despliegue (On-Premise)

> **Supuestos de partida** (a confirmar con el área de infraestructura de la
> institución antes de la puesta en producción — marcados con 🔶): se asume
> un servidor Linux (Ubuntu Server 22.04/24.04 LTS o similar) con Docker
> instalado, sin política de backup institucional previa sobre la que
> apoyarse, y sin balanceador de carga externo.
>
> **Confirmado (2026-07-29)**: el acceso es exclusivamente LAN/VPN
> institucional, sin exposición directa a Internet y sin CA institucional
> disponible — ver decisión de TLS más abajo.
>
> **Actualizado (2026-07-30)**: destino externo de backups resuelto —
> carpeta compartida NFS dedicada en el QNAP institucional (CATE911-NAS,
> `10.52.12.56`), ver sección Backups. Configuración en
> `docker/docker-compose.backup.yml`, 🔶 **todavía sin verificar
> end-to-end**.
>
> **Actualizado (2026-08-05)**: VM de producción instalada — Ubuntu Server
> 26.04 LTS (codename `resolute`), hostname `ige-informes`, IP
> `192.168.70.50`, usuario `igeadmin`. Docker Engine 29.7.1 + Compose
> plugin v5.4.0 y `nfs-common` instalados y verificados (`docker run
> hello-world` OK). Export NFS del QNAP verificado con
> `showmount -e 10.52.12.56`: la carpeta se expone como
> `/ige-informes-backups` (sin el prefijo `RESGUARDO/`), permiso ya
> autorizado para `192.168.70.50` — `docker-compose.backup.yml`
> actualizado con el path correcto. Pendiente: clonar el repo en la VM,
> configurar `.env` de producción y correr el simulacro de restauración
> real contra este mount antes de dar por cerrado el 🔶 de backups.

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
  institucional o vía VPN si hay usuarios remotos. **Confirmado (2026-07-29)**:
  no hay acceso fuera de la red institucional.
- TLS terminado en el reverse proxy. **Decisión tomada (2026-07-29)**: se usa
  `tls internal` de Caddy (CA local autofirmada, generada automáticamente por
  Caddy — ver `docker/Caddyfile`), sin CA institucional, porque el acceso es
  exclusivamente LAN/VPN interna y no hay una CA real disponible. Cada
  cliente (navegador) que acceda por primera vez debe confiar manualmente en
  el certificado autofirmado (advertencia normal de Chrome/Firefox la
  primera vez) — no hay distribución automatizada de la CA a los equipos
  cliente en esta v1, queda a cargo de cada usuario/soporte técnico
  aceptarla una vez. Si en el futuro la institución provee una CA propia o
  se decide exponer el servicio con un dominio público, cambiar el bloque
  `tls internal` del Caddyfile por `tls /ruta/cert.pem /ruta/key.pem` (CA
  institucional) o quitar la directiva `tls` por completo (Let's Encrypt
  automático de Caddy, requiere dominio público resolviendo al servidor).
  Nunca HTTP plano, ni siquiera dentro de la LAN.

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
  que falla no sirve. **Resuelto (2026-07-30)**: destino externo es un QNAP
  institucional (CATE911-NAS, modelo TS-431K, IP `10.52.12.56`), carpeta
  compartida NFS dedicada `ige-informes-backups` sobre el volumen
  `RESGUARDO`, exportada por NFSv3 con acceso restringido a la IP de la VM
  de producción (`192.168.70.50`, confirmada 2026-08-05 tras la instalación
  de Ubuntu Server), lectura/escritura, `sync`,
  `no_root_squash` (necesario porque el sidecar de backup escribe como root
  dentro del contenedor). El volumen `backups` de
  `docker-compose.backup.yml` usa el driver NFS nativo de Docker apuntando
  a ese export, con mount `soft` (para que una caída del QNAP no cuelgue el
  contenedor de backup indefinidamente, solo falle ese backup puntual).
  🔶 **Todavía sin verificar end-to-end contra la VM de producción real**
  (bloqueado por la instalación de esa VM, ver memoria del proyecto) — antes
  de confiar en este destino, repetir el simulacro de restauración ya
  validado localmente pero apuntando al mount NFS real.
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
- El volumen `backups` del compose está configurado como mount NFS al QNAP
  institucional (ver arriba) — en un entorno de desarrollo local sin acceso
  a ese QNAP, `docker-compose.backup.yml` no va a poder montar el volumen;
  usar el compose base solo (`docker-compose.yml`) sin el override de
  backup para desarrollo.
- **Requisito del host** (nuevo, VM de producción): el driver de volumen
  NFS de Docker delega el mount al cliente NFS del kernel del host, así que
  el paquete `nfs-common` debe estar instalado en el servidor Ubuntu antes
  de `docker compose ... up -d` con este override —
  `sudo apt install -y nfs-common`. Sin él, Docker falla al crear el
  volumen con un error de mount, no al arrancar el contenedor.
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
