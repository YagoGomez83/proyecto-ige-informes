# 07b · Plan de Contingencia — Despliegue en Windows Server sin Docker

> **Estado**: plan de respaldo, no el camino principal. El despliegue de
> referencia sigue siendo `07-plan-despliegue.md` (Linux + Docker Compose).
> Este documento existe para el escenario en que la VM de producción
> Linux/Docker no pueda habilitarse a tiempo (ver memoria del proyecto,
> bloqueo del panel ESXi) y haya que operar temporal o permanentemente
> sobre la máquina Windows Server 2016 a la que se consiguió acceso vía
> escritorio remoto.

## 0. Contexto de la máquina disponible

Windows Server 2016 Standard, 8 GB RAM, 360 GB de disco disponible, acceso
por RDP, **sin Docker Desktop instalado ni instalable** (Windows Server
2016 no soporta contenedores Linux con Docker Desktop, y WSL2 —
requisito de Docker Desktop moderno— no está disponible en esa versión de
Windows Server).

La máquina **no está virgen**: ya aloja otro sistema (`portal-911`,
PHP 8.4 vía Laragon) y ya tiene precedentes reales de las dos piezas más
delicadas de este plan, verificados por capturas de esa misma máquina:

- **PostgreSQL 17** ya instalado y corriendo como servicio de Windows
  nativo (`postgresql-x64-17`, inicio automático, cuenta "Servicio de
  red") — confirmar si es una instancia reusable para IGE o si hace falta
  una instalación separada (ver sección 2).
- **Una API .NET ya empaquetada como servicio de Windows** con NSSM
  (`MotorcycleManagerAPI`, inicio automático, corriendo) — confirma que el
  patrón "Kestrel + NSSM" ya funciona en esa máquina concretamente, no
  solo en teoría. Este plan reusa exactamente ese patrón para IGE.
- **NSSM 2.24** descargado (2014, es la última release estable oficial —
  no hace falta actualizar, pero conviene bajar una copia limpia desde
  [nssm.cc](https://nssm.cc) para no depender de un `.zip` de hace más de
  10 años en Descargas).
- `MySQL80` también corre como servicio — no lo usa IGE (que es Postgres),
  pero confirma que ya hay más de un motor de base de datos conviviendo
  ahí; prestar atención a puertos/nombres para no chocar.

**Decisión ya tomada con el usuario**: el reverse proxy va a ser
**nginx standalone para Windows** (no Laragon, no IIS/ARR) — instalado y
gestionado por separado del Laragon que ya sirve `portal-911`, para no
acoplar el ciclo de vida de ambos sistemas. Mismo rol que cumple Caddy hoy
en el plan Linux: terminación TLS + reverse proxy hacia Kestrel.

## 1. Mapeo de componentes: Docker (plan principal) → Windows nativo

| Componente Docker | Rol | Equivalente nativo Windows | Estado en esta máquina |
|---|---|---|---|
| `reverse-proxy` (Caddy) | TLS + reverse proxy + headers de seguridad | **nginx for Windows** + servicio vía NSSM | A instalar (decisión tomada: nginx, no IIS/Laragon) |
| `web` (Blazor Server) | La aplicación | `dotnet publish` (framework-dependent) + servicio vía NSSM | Patrón ya probado en esta máquina (`MotorcycleManagerAPI`) |
| `postgres` | Base de datos | **PostgreSQL for Windows** (instalador oficial EDB) | **Ya instalado y corriendo** (`postgresql-x64-17`) |
| `minio` | Storage S3-compatible (PDFs/imágenes) | **MinIO Server for Windows** (`minio.exe` nativo) + servicio vía NSSM | A instalar |
| `clamav` | Escaneo antivirus de archivos subidos | **ClamWin** o **ClamAV for Windows** (build oficial) + servicio | A instalar — ver nota de riesgo en sección 4 |
| — (no existe hoy) | Worker Hangfire | No aplica — el `worker` del `07-plan-despliegue.md` es aspiracional, no existe en el código actual | — |

No hace falta reemplazar 5 piezas, son 4 reales (el "worker" del doc
principal todavía no se implementó).

## 2. PostgreSQL — confirmar antes de asumir nada

El servicio `postgresql-x64-17` que ya corre en esa máquina **puede ser de
otro sistema** (posiblemente del propio `portal-911` u otra app previa).
Antes de usarlo para IGE:

1. Confirmar la versión exacta (17 coincide con lo que usa el proyecto en
   Docker — `postgres:16` en el compose actual, ver nota de compatibilidad
   abajo) y si acepta crear una base de datos nueva sin interferir con lo
   que ya tenga cargado.
2. **Crear una base de datos y un rol dedicados para IGE**
   (`ige_informes` / usuario `ige_informes` con password propio), nunca
   reusar un rol/base de otro sistema — aislamiento mínimo aunque
   compartan el mismo motor.
3. **Nota de compatibilidad de versión**: el proyecto desarrolla y prueba
   contra `postgres:16` (Docker) y Testcontainers también usa
   `postgres:16-alpine` en los tests de integración. PostgreSQL 17 es
   compatible hacia adelante para el uso que hace el proyecto (EF Core
   Npgsql, sin extensiones exóticas más allá de `unaccent`, que existe en
   ambas versiones) — no debería haber problema real, pero es una
   variable no probada explícitamente en este proyecto. Si aparece algún
   comportamiento raro de EF Core/migraciones, es el primer sospechoso a
   descartar.
4. Confirmar que el servicio escucha en `localhost`/la interfaz correcta
   y que `pg_hba.conf` permite la conexión desde donde va a correr `web`
   (si todo corre en la misma máquina, `localhost` alcanza — no hace falta
   exponer el puerto 5432 a la red).

## 3. MinIO — la pieza menos probada de este plan

MinIO Server distribuye un binario standalone para Windows
(`minio.exe`), sin dependencias de contenedor — es el mismo servidor que
corre en Docker, solo que ejecutado directo. Pasos:

1. Descargar `minio.exe` (release oficial de MinIO para Windows amd64).
2. Definir una carpeta de datos dedicada (ej.
   `D:\IGE\minio-data`) — equivalente al volumen `miniodata` de Docker.
3. Configurar `MINIO_ROOT_USER`/`MINIO_ROOT_PASSWORD` como variables de
   entorno del servicio (NSSM permite definir variables de entorno por
   servicio sin tocar las globales del sistema).
4. Registrar como servicio de Windows vía NSSM, igual que la app .NET:
   `nssm install IGE-MinIO "C:\ruta\minio.exe" "server D:\IGE\minio-data --console-address :9001"`.
5. El bucket `ige-informes` se crea solo (la app lo asegura en el primer
   `SubirAsync`, ver `MinioFileStorage.AsegurarBucketAsync` — no requiere
   paso manual).
6. **Punto de atención real**: `Minio__EndpointPublico` (la variable que
   hoy resuelve las URLs prefirmadas para que el navegador del cliente
   pueda ver el PDF embebido) tiene que apuntar a la IP/nombre de host
   real de la máquina Windows en la red institucional, puerto 9000 —
   mismo mecanismo que ya está resuelto en el plan Linux
   (`MINIO_ENDPOINT_PUBLICO`), no es nada nuevo de este escenario, pero
   hay que volver a configurarlo con la IP de esta máquina específica.

**Este es el componente menos verificado del plan** — no hay precedente
de MinIO nativo en Windows en esta máquina (a diferencia de PostgreSQL y
de .NET-como-servicio, que ya están probados). Antes de confiar en este
plan para producción real, conviene hacer una prueba de humo completa
(subir un PDF, confirmar que el visor embebido lo muestra) apenas esté
instalado.

## 4. ClamAV — el componente más débil del plan de contingencia

Este es el punto donde el plan Windows es genuinamente más frágil que el
plan Docker, y conviene decirlo con claridad en vez de minimizarlo:

- La imagen `clamav/clamav:stable` de Docker es un build oficial,
  mantenido activamente, con `freshclam` (actualización de firmas)
  integrado y probado.
- En Windows, las opciones son **ClamWin** (más orientado a escritorio,
  con GUI, no pensado primariamente para correr como daemon de servicio) o
  compilar/usar un build no oficial de ClamAV para Windows con `clamd`
  (el daemon que habla el protocolo INSTREAM que ya usa
  `ClamAvAntivirusScanner` vía TCP). Ninguna de las dos tiene el mismo
  nivel de mantenimiento/confianza que la imagen Docker oficial.
- El código ya es **fail-closed** ante un antivirus no disponible
  (`AntivirusNoDisponibleException` se propaga sin capturar, rechaza la
  carga) — esto es una ventaja real: si ClamAV en Windows resulta
  inestable, el sistema no queda vulnerable silenciosamente, simplemente
  deja de aceptar cargas de PDF/imágenes hasta que se resuelva. Prioriza
  seguridad sobre disponibilidad, que es la postura correcta para este
  tipo de dato.

**Recomendación**: si este plan se activa, dedicarle tiempo extra
específicamente a probar `clamd` en Windows contra el protocolo INSTREAM
real (no asumir que "ClamAV para Windows" es intercambiable sin pruebas)
antes de dar por cerrada la migración — es el componente con más
probabilidad de requerir ajustes no anticipados acá.

## 5. La aplicación (`IGE.Informes.Web`) como servicio de Windows

Ya hay un patrón probado en esta máquina (`MotorcycleManagerAPI`), así que
esta parte es la de menor riesgo del plan:

1. `dotnet publish src/IGE.Informes.Web/IGE.Informes.Web.csproj -c Release -o C:\IGE\web --self-contained false`
   — **framework-dependent**, no self-contained: requiere el
   **ASP.NET Core Runtime 10.0 (Hosting Bundle)** instalado en el
   servidor, más liviano que empaquetar el runtime completo con cada
   deploy. Confirmar que la versión del SDK/runtime del proyecto (.NET 10)
   tiene ya release estable para Windows Server 2016 al momento de
   ejecutar esto — .NET Core moderno soporta Windows Server 2016 como
   plataforma, pero conviene confirmar la matriz de soporte de la versión
   exacta antes de comprometerse.
2. Variables de entorno equivalentes a las del `docker-compose.yml`
   (`ConnectionStrings__Default`, `Minio__*`, `ClamAv__*`,
   `IGE_ADMIN_EMAIL`/`IGE_ADMIN_PASSWORD` solo la primera vez) — definidas
   como variables de entorno del servicio de Windows vía NSSM (`nssm set
   IGE-Web AppEnvironmentExtra ...`), nunca hardcodeadas en
   `appsettings.json` commiteado (mismo principio que ya rige en
   `CLAUDE.md` para el plan Linux).
3. `ASPNETCORE_URLS=http://localhost:5000` (o el puerto que se decida) —
   Kestrel sirviendo solo en loopback, igual criterio que hoy (`web:8080`
   nunca se publica directo, solo el reverse proxy sale a la red).
4. Registrar con NSSM:
   `nssm install IGE-Web "C:\Program Files\dotnet\dotnet.exe" "C:\IGE\web\IGE.Informes.Web.dll"`,
   tipo de inicio automático, reinicio ante fallo configurado en la
   pestaña "Exit actions" de NSSM (equivalente a `restart: unless-stopped`
   de Docker).
5. Las migraciones de EF Core corren igual que en Linux (automáticas al
   iniciar, o vía `dotnet ef database update` explícito) — no cambia nada
   específico de Windows acá.

## 6. nginx como reverse proxy (reemplaza a Caddy)

Decisión tomada: nginx standalone para Windows, separado del Laragon
existente, registrado también como servicio vía NSSM.

Traducción directa del `Caddyfile` actual (headers de seguridad + proxy +
TLS) a `nginx.conf`:

```nginx
server {
    listen 443 ssl;
    server_name _;

    ssl_certificate      C:/IGE/nginx/certs/ige.crt;
    ssl_certificate_key  C:/IGE/nginx/certs/ige.key;

    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-Frame-Options "DENY" always;
    add_header Referrer-Policy "same-origin" always;
    add_header Content-Security-Policy "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; connect-src 'self' wss:; frame-src 'self' https://<IP-servidor>:9000; frame-ancestors 'none'" always;

    location / {
        proxy_pass http://127.0.0.1:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;

        # Blazor Server necesita WebSockets para el circuito SignalR —
        # sin esto la UI carga pero queda "colgada" sin interactividad.
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
    }
}
```

Puntos que **no** son un simple copy-paste del Caddyfile:

- **Certificado TLS**: Caddy con `tls internal` genera y renueva un
  certificado autofirmado solo. nginx no hace esto — hay que generar el
  certificado autofirmado a mano una vez (`openssl req -x509 -newkey
  rsa:2048 ...`, o el propio `New-SelfSignedCertificate` de PowerShell) y
  configurar el renovo manual antes de que expire. Es el mismo modelo de
  confianza ya aceptado (`tls internal`, sin CA institucional, LAN/VPN
  únicamente, cada cliente acepta la advertencia una vez) — solo cambia
  quién genera el certificado.
- **WebSockets**: crítico y fácil de olvidar. Blazor Server depende de
  una conexión SignalR persistente; sin las líneas de `Upgrade`/
  `Connection "upgrade"` la app parece cargar pero no responde a ningún
  click. Caddy hace esto automáticamente con `reverse_proxy`, nginx
  requiere las líneas explícitas de arriba.
- Registrar nginx como servicio: nginx para Windows no trae un instalador
  de servicio nativo — se usa NSSM otra vez (`nssm install IGE-Nginx
  "C:\nginx\nginx.exe"`, con el working directory apuntando a la carpeta
  de nginx para que encuentre su config relativa).

## 7. Backups

El mecanismo de backup actual (`pg_dump` + `mc mirror` a un destino
externo, ver `07-plan-despliegue.md`) se traduce así:

- `pg_dump.exe` viene con la instalación de PostgreSQL para Windows — un
  script `.ps1` + el Programador de tareas de Windows reemplaza al cron
  del sidecar Docker.
- `mc.exe` (cliente de MinIO) también tiene build nativo para Windows,
  mismo comando `mc mirror` que hoy.
- **El destino externo NFS (QNAP, `10.52.12.56`) no es directamente
  accesible desde Windows sin un cliente NFS** (Windows Server tiene un
  rol opcional "Client for NFS", pero es otra pieza a instalar y probar).
  Alternativa más simple en Windows: copiar el backup a una carpeta SMB
  compartida en el mismo QNAP (la mayoría de NAS exponen el mismo volumen
  por NFS y por SMB/CIFS simultáneamente) — evita instalar el rol NFS,
  usa `robocopy` o `Copy-Item` estándar de PowerShell contra un share de
  red mapeado.
- Mismo principio que ya rige: el backup nunca vive solo en el disco de
  esta máquina.

## 8. Pros y contras frente al plan Linux/Docker

### Pros de este plan de contingencia

- **Reduce la dependencia de que se resuelva el acceso a la VM ESXi** —
  no bloquea el avance del proyecto mientras esa gestión de
  infraestructura sigue trabada.
- **Dos de las cuatro piezas ya están probadas en esta máquina
  concretamente** (PostgreSQL nativo, .NET como servicio con NSSM) — no
  es una apuesta completamente en el aire, hay evidencia real de que
  funciona en este entorno específico.
- Sin Docker de por medio, hay una capa menos de abstracción para
  depurar si algo falla — a cambio de más piezas nativas distintas que
  mantener por separado (ver contras).
- Windows Server con GUI + RDP es más cómodo para quien no tiene
  experiencia operando Linux por SSH, si el soporte de esa máquina
  eventualmente recae en otra persona del organismo.

### Contras / riesgos reales

- **Pierde toda la reproducibilidad del `docker-compose.yml`.** Hoy "el
  entorno" es un archivo versionado; en este plan, "el entorno" son 4
  servicios instalados a mano, cada uno con su propia superficie de
  configuración, sin un solo comando que reconstruya todo desde cero. El
  riesgo de "funciona en mi máquina pero no sé por qué" sube
  considerablemente.
- **ClamAV es la pieza débil real** (sección 4) — es el único componente
  sin un camino nativo de Windows tan sólido como su contraparte Docker.
- **MinIO nativo en Windows no tiene precedente en esta máquina** — es la
  segunda pieza de mayor incertidumbre, aunque el riesgo técnico en sí es
  bajo (mismo binario que en Docker).
- **Actualizaciones más manuales**: el flujo `docker compose pull && up
  -d` de un deploy se convierte en parar 2-4 servicios de Windows,
  reemplazar binarios/carpetas a mano, y volver a arrancar — más pasos,
  más superficie de error humano, sin el aislamiento que da reconstruir
  una imagen desde cero.
- **Esta máquina ya aloja otro sistema** (`portal-911`) — cualquier
  problema de recursos (los 8 GB de RAM son ajustados repartidos entre
  PostgreSQL + MinIO + IGE.Web + nginx + lo que ya consume Laragon/PHP)
  puede terminar afectando a ambos sistemas. En el plan Linux, la VM está
  dedicada solo a IGE.
- **Compatibilidad de versión de PostgreSQL sin validar** (17 vs. el 16
  usado en desarrollo/tests) — riesgo bajo pero real, sección 2.
- Si más adelante se resuelve el acceso a la VM Linux, **hay que migrar
  los datos otra vez** (dump de Postgres + copia de objetos de MinIO desde
  Windows hacia los contenedores Linux) — trabajo extra que no existiría
  si el plan principal hubiera arrancado directo.

### Recomendación

Es un plan de contingencia razonable y ejecutable, no una improvisación
— dos de sus cuatro piezas centrales ya tienen precedente probado en esa
máquina específica. Si el bloqueo de la VM ESXi se extiende más de lo
esperable, vale la pena avanzarlo en paralelo (empezar por MinIO y
ClamAV, que son las piezas nuevas de mayor incertidumbre, mientras se
sigue insistiendo por el acceso a la VM) en vez de esperar a que sea la
única opción bajo presión de tiempo.

Si se termina activando en producción real (no solo como respaldo
temporal), documentar en este mismo archivo cualquier ajuste concreto que
aparezca al ejecutarlo — este plan es una guía previa a la ejecución, no
un relato de una ejecución ya hecha.
