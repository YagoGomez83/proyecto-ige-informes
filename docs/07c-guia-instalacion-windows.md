# 07c · Guía de Instalación Paso a Paso — Windows Server (Contingencia)

> Ejecutar **solo si se activa el plan de contingencia** descripto en
> `07b-plan-despliegue-contingencia-windows.md` — leer ese documento
> primero para entender las decisiones y los riesgos de cada componente
> antes de ejecutar esto a ciegas. Esta guía asume la máquina Windows
> Server 2016 Standard ya descripta ahí (8 GB RAM, 360 GB disco, acceso
> RDP, PostgreSQL 17 y NSSM ya presentes).

**Convención de rutas**: todo bajo `C:\IGE\`. Ajustar si en el momento de
ejecutar se decide otra unidad.

**Orden**: seguir las secciones en el orden en que están — cada una
depende de que la anterior esté verificada y funcionando. No saltar al
paso siguiente si el paso de verificación de la sección actual falla.

---

## 0. Preparación

1. Crear la carpeta raíz: `New-Item -ItemType Directory -Path C:\IGE -Force`
2. Confirmar versión de PowerShell disponible: `$PSVersionTable.PSVersion`
   (Windows Server 2016 trae PowerShell 5.1 por defecto — todos los
   comandos de esta guía asumen 5.1, no PowerShell 7+).
3. Descargar una copia limpia de NSSM desde
   [nssm.cc/download](https://nssm.cc/download) (la carpeta `nssm-2.24`
   ya presente en Descargas es de 2014 pero es la última release estable
   — usarla está bien, solo confirmar que el `.exe` de 64 bits
   (`nssm-2.24\win64\nssm.exe`) corre: `& "C:\...\nssm.exe" version`).
4. Copiar `nssm.exe` (la variante `win64`) a `C:\IGE\nssm.exe` para tener
   una ruta corta y estable en el resto de esta guía.

**Verificación**: `C:\IGE\nssm.exe version` debe imprimir `NSSM 2.24`.

---

## 1. PostgreSQL — base de datos dedicada para IGE

El servicio `postgresql-x64-17` ya existente en esta máquina es de otro
sistema — **no reusar su base de datos**, crear una nueva dedicada a IGE
en la misma instancia del motor.

1. Abrir `psql` (viene con la instalación existente, buscar
   `C:\Program Files\PostgreSQL\17\bin\psql.exe` si no está en el PATH) o
   `pgAdmin` si está instalado, y conectar como el superusuario `postgres`
   (va a pedir la contraseña que se haya definido cuando se instaló ese
   Postgres — si no se conoce, hay que recuperarla o resetearla antes de
   seguir, no continuar sin ella).
2. Crear el rol y la base dedicados a IGE:
   ```sql
   CREATE USER ige_informes WITH PASSWORD 'REEMPLAZAR_POR_PASSWORD_FUERTE';
   CREATE DATABASE ige_informes OWNER ige_informes;
   ```
3. Confirmar que la extensión `unaccent` (usada por la búsqueda
   combinada) está disponible:
   ```sql
   \c ige_informes
   CREATE EXTENSION IF NOT EXISTS unaccent;
   ```
   (Esto también lo hace la propia app al arrancar vía
   `AppDbContext.OnModelCreating` + migraciones, pero confirmarlo acá
   detecta temprano si el rol `ige_informes` no tiene permiso de crear
   extensiones — si falla, correr este paso como `postgres` en vez de
   como `ige_informes`, y luego dar el `OWNER` sobre la extensión si hace
   falta.)
4. Confirmar que el servicio escucha en `localhost` (si la app va a
   correr en la misma máquina, no hace falta tocar `postgresql.conf`
   `listen_addresses` ni exponer el puerto 5432 a la red — dejarlo en su
   configuración por defecto de esa instancia existente).
5. Revisar `pg_hba.conf` (típicamente en
   `C:\Program Files\PostgreSQL\17\data\pg_hba.conf`) — confirmar que hay
   una línea que permita conexión desde `127.0.0.1`/`localhost` con
   `scram-sha-256` o `md5` para el rol `ige_informes` (la instalación
   default de PostgreSQL para Windows generalmente ya cubre esto para
   conexiones locales, pero confirmarlo evita perder tiempo debuggeando
   un "connection refused" más adelante).

**Verificación**: `psql -h localhost -U ige_informes -d ige_informes -c "SELECT 1;"`
(va a pedir la password del paso 2) debe devolver `1` sin errores.

**Anotar** (para usar en la sección 5): cadena de conexión
`Host=localhost;Port=5432;Database=ige_informes;Username=ige_informes;Password=<la definida>`.

---

## 2. MinIO — storage de PDFs/imágenes

1. Descargar el binario oficial:
   `Invoke-WebRequest -Uri "https://dl.min.io/server/minio/release/windows-amd64/minio.exe" -OutFile "C:\IGE\minio.exe"`
2. Crear la carpeta de datos: `New-Item -ItemType Directory -Path C:\IGE\minio-data -Force`
3. Definir credenciales fuertes para el root de MinIO — **anotarlas**,
   se necesitan en el paso 5 (`Minio__AccessKey`/`Minio__SecretKey`).
4. Registrar el servicio con NSSM:
   ```powershell
   C:\IGE\nssm.exe install IGE-MinIO "C:\IGE\minio.exe" "server C:\IGE\minio-data --console-address :9001"
   C:\IGE\nssm.exe set IGE-MinIO AppEnvironmentExtra "MINIO_ROOT_USER=<usuario_elegido>" "MINIO_ROOT_PASSWORD=<password_elegida>"
   C:\IGE\nssm.exe set IGE-MinIO Start SERVICE_AUTO_START
   C:\IGE\nssm.exe set IGE-MinIO AppExit Default Restart
   ```
5. Arrancar: `Start-Service IGE-MinIO` (o `net start IGE-MinIO`).
6. Abrir el puerto 9000 en el Firewall de Windows para la red LAN
   institucional (el navegador de cada cliente necesita llegar a este
   puerto directamente para las URLs prefirmadas de descarga/preview):
   ```powershell
   New-NetFirewallRule -DisplayName "IGE MinIO" -Direction Inbound -Protocol TCP -LocalPort 9000 -Action Allow
   ```
   **No** abrir el 9001 (consola de administración) a la red — dejarlo
   accesible solo desde `localhost` o vía RDP a esta misma máquina.

**Verificación**: desde un navegador en esa misma máquina,
`http://localhost:9000/minio/health/live` debe responder `200 OK`. Desde
otra máquina de la LAN, `http://<IP-del-servidor>:9000/minio/health/live`
también debe responder — si no, revisar el firewall (paso 6).

**Anotar**: `Minio__Endpoint=localhost:9000`,
`Minio__EndpointPublico=<IP-del-servidor>:9000`, más
usuario/password del paso 3.

---

## 3. ClamAV — escaneo antivirus

Esta es la sección de mayor incertidumbre del plan (ver
`07b-plan-despliegue-contingencia-windows.md`, sección 4) — dedicarle
tiempo extra y no asumir que va a funcionar igual que en Docker.

1. Descargar un build de ClamAV para Windows con soporte de `clamd`
   (el daemon, no solo el escáner de línea de comandos `clamscan`) —
   confirmar en el momento de ejecutar cuál es la fuente oficial vigente,
   ya que los builds de Windows de ClamAV no siempre están al mismo nivel
   de mantenimiento que la imagen Docker.
2. Instalar y localizar `clamd.conf` — configurar como mínimo:
   - `TCPSocket 3310` (mismo puerto que usa hoy el contenedor Docker,
     coincide con `ClamAv__Port=3310` ya usado en la app).
   - `TCPAddr 127.0.0.1` (el daemon solo necesita ser alcanzable desde la
     misma máquina donde corre `web`, no exponerlo a la LAN).
3. `freshclam` (actualización de firmas) — confirmar que hay una tarea
   programada o un mecanismo equivalente al `freshclam` automático que
   trae la imagen Docker; sin firmas actualizadas, el escaneo pierde
   valor con el tiempo. Considerar el Programador de tareas de Windows
   corriendo `freshclam.exe` diariamente.
4. Registrar `clamd` como servicio (si el build elegido no trae su propio
   instalador de servicio, usar NSSM igual que con los demás
   componentes).

**Verificación**: probar el protocolo INSTREAM manualmente antes de
integrarlo con la app — por ejemplo con `telnet localhost 3310` seguido
del comando `PING` (`clamd` debe responder `PONG`), o con una prueba más
completa enviando un archivo de test EICAR (el archivo estándar de
prueba de antivirus, inofensivo, diseñado para que cualquier antivirus lo
detecte como "amenaza" de prueba) para confirmar que el INSTREAM
realmente escanea contenido y no solo responde al PING.

**Si esta sección no puede cerrarse de forma confiable**: recordar que
el sistema es fail-closed (`AntivirusNoDisponibleException` rechaza la
carga sin persistir nada) — es preferible dejar la carga de PDFs/imágenes
temporalmente no disponible a integrar un ClamAV en el que no se confía.
No relajar el código para tolerar su ausencia.

**Anotar**: `ClamAv__Host=localhost`, `ClamAv__Port=3310`.

---

## 4. nginx — reverse proxy y TLS

1. Descargar nginx for Windows (mainline) y descomprimir en `C:\IGE\nginx\`.
2. Generar el certificado autofirmado (mismo modelo de confianza que
   `tls internal` de Caddy en el plan Linux — LAN/VPN institucional
   únicamente, sin CA real disponible):
   ```powershell
   New-Item -ItemType Directory -Path C:\IGE\nginx\certs -Force
   $cert = New-SelfSignedCertificate -DnsName "<IP-o-hostname-del-servidor>" -CertStoreLocation "cert:\LocalMachine\My" -NotAfter (Get-Date).AddYears(2)
   # Exportar a formato PEM para nginx requiere OpenSSL o un paso extra de conversión
   # desde .pfx — documentar el comando exacto usado en el momento, según
   # qué herramienta esté disponible en la máquina (openssl.exe suele venir
   # con Git for Windows, ya presente si se usa Git en esa máquina).
   ```
   **Anotar la fecha de expiración** (2 años en el ejemplo) — a
   diferencia de Caddy, que renueva `tls internal` solo, acá el
   renovado es 100% manual.
3. Editar `C:\IGE\nginx\conf\nginx.conf` con el bloque `server` descripto
   en `07b-plan-despliegue-contingencia-windows.md` sección 6 — copiar
   ese bloque tal cual, ajustando:
   - Las rutas de `ssl_certificate`/`ssl_certificate_key` a lo generado
     en el paso 2.
   - El host/puerto de `frame-src` en el `Content-Security-Policy` a la
     IP real del servidor + `:9000` (el mismo valor de
     `Minio__EndpointPublico` de la sección 2).
   - `proxy_pass http://127.0.0.1:5000;` — confirmar que coincide con el
     puerto que se decida para Kestrel en la sección 5.
4. Registrar como servicio con NSSM:
   ```powershell
   C:\IGE\nssm.exe install IGE-Nginx "C:\IGE\nginx\nginx.exe"
   C:\IGE\nssm.exe set IGE-Nginx AppDirectory "C:\IGE\nginx"
   C:\IGE\nssm.exe set IGE-Nginx Start SERVICE_AUTO_START
   C:\IGE\nssm.exe set IGE-Nginx AppExit Default Restart
   ```
5. Abrir el puerto 443 en el firewall:
   ```powershell
   New-NetFirewallRule -DisplayName "IGE HTTPS" -Direction Inbound -Protocol TCP -LocalPort 443 -Action Allow
   ```

**Verificación**: `Start-Service IGE-Nginx` y luego
`Invoke-WebRequest -Uri https://localhost -SkipCertificateCheck` no debe
tirar error de conexión (va a fallar con 502 hasta completar la sección
5, porque todavía no hay nada escuchando en el puerto 5000 — eso es
esperable en este punto).

---

## 5. La aplicación IGE.Informes.Web

1. Confirmar que el **ASP.NET Core Runtime 10.0 Hosting Bundle** está
   instalado en el servidor (no solo el runtime genérico — el Hosting
   Bundle incluye el módulo necesario para hospedar bajo IIS/Kestrel
   correctamente en Windows). Verificar: `dotnet --list-runtimes` debe
   listar `Microsoft.AspNetCore.App 10.0.x`.
2. Desde una máquina con el repo (o copiando el resultado ya compilado a
   esta máquina), publicar:
   ```powershell
   dotnet publish src/IGE.Informes.Web/IGE.Informes.Web.csproj -c Release -o C:\IGE\web --self-contained false
   ```
3. Configurar las variables de entorno del servicio con todo lo anotado
   en las secciones anteriores:
   ```powershell
   C:\IGE\nssm.exe install IGE-Web "C:\Program Files\dotnet\dotnet.exe" "C:\IGE\web\IGE.Informes.Web.dll"
   C:\IGE\nssm.exe set IGE-Web AppDirectory "C:\IGE\web"
   C:\IGE\nssm.exe set IGE-Web AppEnvironmentExtra ^
     "ASPNETCORE_URLS=http://127.0.0.1:5000" ^
     "ASPNETCORE_ENVIRONMENT=Production" ^
     "ConnectionStrings__Default=Host=localhost;Port=5432;Database=ige_informes;Username=ige_informes;Password=<de la sección 1>" ^
     "Minio__Endpoint=localhost:9000" ^
     "Minio__EndpointPublico=<IP-servidor>:9000" ^
     "Minio__AccessKey=<de la sección 2>" ^
     "Minio__SecretKey=<de la sección 2>" ^
     "Minio__BucketName=ige-informes" ^
     "Minio__UseSsl=false" ^
     "ClamAv__Host=localhost" ^
     "ClamAv__Port=3310" ^
     "IGE_ADMIN_EMAIL=admin@ige.local" ^
     "IGE_ADMIN_PASSWORD=<definir_password_fuerte_solo_para_el_primer_arranque>"
   C:\IGE\nssm.exe set IGE-Web Start SERVICE_AUTO_START
   C:\IGE\nssm.exe set IGE-Web AppExit Default Restart
   ```
   (La sintaxis exacta de `AppEnvironmentExtra` con múltiples variables
   puede requerir ajuste según la versión de NSSM — si falla con varias
   en una sola llamada, setearlas una por una o vía la GUI de NSSM:
   `C:\IGE\nssm.exe edit IGE-Web`.)
4. **Nunca dejar `IGE_ADMIN_PASSWORD` en texto plano de forma permanente**
   — solo debe estar presente hasta que se cree el primer usuario Admin;
   después, quitarla de la configuración del servicio (mismo criterio que
   ya documenta `docker/.env.example`).
5. Crear la carpeta de claves de Data Protection (equivalente al volumen
   `dataprotection_keys` de Docker — sin esto, cada reinicio del servicio
   invalida las cookies de sesión de todos los usuarios logueados):
   `New-Item -ItemType Directory -Path C:\IGE\keys -Force`
   — y confirmar en el código/configuración que la app apunta ahí (revisar
   `Program.cs` de `IGE.Informes.Web` para el `PersistKeysToFileSystem`
   configurado, o agregarlo si no está parametrizado por variable de
   entorno).
6. Arrancar: `Start-Service IGE-Web`.
7. Las migraciones de EF Core deberían aplicarse automáticamente al
   arrancar (mismo comportamiento que en Docker) — revisar el log de
   arranque (ver sección 6) para confirmar que no hay errores de
   conexión a la base antes de dar el paso por cerrado.

**Verificación**: `Invoke-WebRequest -Uri http://localhost:5000/health`
debe responder `200 OK` directo contra Kestrel. Después,
`Invoke-WebRequest -Uri https://localhost -SkipCertificateCheck` (a
través de nginx) también debe responder — si el primero funciona pero el
segundo no, el problema está en la configuración de nginx (sección 4),
no en la app.

---

## 6. Verificación end-to-end

Antes de considerar el entorno productivo:

1. Desde un navegador de otra máquina en la LAN (no la del servidor),
   entrar a `https://<IP-del-servidor>` — aceptar la advertencia de
   certificado autofirmado una vez.
2. Iniciar sesión con el Admin creado (`IGE_ADMIN_EMAIL`).
3. Cargar un PDF de prueba en un Informe — confirma en un solo paso que
   PostgreSQL, MinIO y ClamAV están todos correctamente conectados (si
   cualquiera de los tres falla, este paso lo va a mostrar).
4. Abrir el detalle de ese Informe y confirmar que el visor de PDF
   embebido carga (confirma que `Minio__EndpointPublico` y el `frame-src`
   del CSP de nginx están bien configurados — es el punto más propenso a
   quedar mal apuntado).
5. Cerrar sesión, reiniciar el servicio `IGE-Web`
   (`Restart-Service IGE-Web`), volver a entrar sin tener que loguearse
   de nuevo con una sesión que ya estaba activa en otra pestaña —
   confirma que Data Protection Keys (sección 5, paso 5) están
   persistiendo correctamente.

## 7. Registro de ejecución real

Esta guía es una planificación previa — al ejecutarla de verdad, agregar
acá (o en un archivo aparte referenciado desde este) cualquier desvío
real encontrado: versión exacta de ClamAV que terminó funcionando,
ajustes de `pg_hba.conf` que hicieron falta, problemas de permisos de
NSSM, etc. — para que la próxima vez (o si hay que reconstruir esta
máquina) no haya que redescubrir lo mismo.
