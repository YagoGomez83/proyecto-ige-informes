# 06 · Seguridad y Modelo de Amenazas

## Clasificación de datos

Este sistema maneja **datos sensibles de investigaciones judiciales/policiales
en curso**: causas, DNI de denunciantes/damnificados/sospechosos, patentes de
vehículos, imágenes de personas y recorridos. Se lo trata con el mismo
estándar de cuidado que un sistema de salud o financiero, aunque no exista
una ley específica citada por el equipo — es la postura por defecto dada la
naturaleza del dato.

| Dato | Clasificación | Implicancia |
|---|---|---|
| DNI, nombre de Persona | Dato personal sensible | Acceso restringido + auditoría de lectura |
| Imágenes de personas/vehículos | Dato personal sensible | Acceso restringido, URLs firmadas con expiración |
| Relato/observaciones de Caso e Informe | Información de investigación en curso | Confidencial, acceso solo a roles autorizados |
| Carátula/pieza sumarial | Dato de expediente | Confidencial |
| Credenciales de usuario | Secreto | Hash Argon2id, nunca en logs |

## Modelo de amenazas (STRIDE) por componente

### 1. Autenticación / gestión de usuarios

| Amenaza | Riesgo | Control |
|---|---|---|
| **S**poofing — suplantación de identidad | Alto (sin AD/LDAP, credenciales propias) | Hash de contraseñas con **Argon2id**; política de contraseña mínima (12+ caracteres); **2FA (TOTP) obligatorio para Supervisor y Admin** — tras un login con contraseña correcta, si el rol es Supervisor/Admin y `TwoFactorEnabled = false`, se lo redirige a configurarlo (`Account/ConfigurarDosFactoresObligatorio`) antes de poder acceder a cualquier otra página; no hay forma de posponerlo ni saltearlo. Para Analista sigue siendo autoservicio opcional (`Manage/EnableAuthenticator`) |
| **S**poofing — fuerza bruta / credential stuffing | Alto | Bloqueo de cuenta tras 5 intentos fallidos (lockout progresivo, `Identity.Lockout`); rate limiting por IP en las rutas `/Account` (10 req/min, `Microsoft.AspNetCore.RateLimiting`, ver `src/IGE.Informes.Web/Program.cs`) — requiere `ForwardedHeaders` configurado para que la IP particionada sea la del cliente real y no la del reverse proxy, ver Fase 5 |
| **E**levation of Privilege | Medio | RBAC estricto por policy (`Analista` / `Supervisor` / `Admin`) validado en el backend en cada Command/Query — nunca confiar en el rol mostrado en el cliente |
| **E**levation of Privilege — circuito de Blazor Server con permisos obsoletos tras bloqueo/cambio de rol | Medio (mitigado) | `BloquearAsync`/`CambiarRolAsync` (`UserManagementService`) invalidan el `SecurityStamp` del usuario; `PersistingRevalidatingAuthenticationStateProvider` lo revalida cada 30 min y cierra el circuito si no coincide — evita que una sesión ya abierta siga operando con el rol/estado viejo indefinidamente. Latencia de hasta 30 min, no es corte instantáneo (ver `docs/09-onboarding-offboarding-usuarios.md`) |
| **R**epudiation | Medio | Toda acción de login/logout queda en `AuditLog` con IP y user-agent |

### 2. Acceso a datos (Informes, Casos, Vehículos, Personas)

| Amenaza | Riesgo | Control |
|---|---|---|
| **I**nformation Disclosure — acceso no autorizado a un Informe/Persona | Alto | Autorización a nivel de Application layer (no solo UI); todo acceso de lectura a `Informe`, `CasoAnalisis`, `Vehiculo`, `Persona` se registra en `AuditLog` (usuario, entidad, timestamp) — es requisito, no opcional |
| **T**ampering — modificación no autorizada de un Informe publicado | Alto | Un `Informe` en estado `Publicado` es inmutable salvo por un flujo explícito de "corrección" que genera nueva versión y conserva el historial (nunca sobreescribe) |
| **T**ampering — código de identificación repetido entre entidades (`Camara.Codigo`) | Bajo | Decisión de diseño explícita (Fase 7): a diferencia del resto de los identificadores de negocio del proyecto, `Camara.Codigo` **no** es único — el relevamiento real trae códigos repetidos entre cámaras de una misma instalación agrupada (ver `docs/01-glosario-dominio.md`). No representa una amenaza de seguridad (no es un identificador de acceso ni de autorización), solo una particularidad de modelado a tener presente si se agrega lógica nueva que asuma unicidad |
| **R**epudiation — negar haber modificado/consultado algo | Medio | `AuditLog` append-only, sin permisos de borrado ni para Admin desde la aplicación |
| **I**nformation Disclosure — archivos (PDFs/imágenes) accesibles por URL directa | Alto | MinIO con **URLs prefirmadas de corta expiración** (nunca URLs públicas permanentes); bucket privado por defecto |

### 3. Ingesta de PDFs (upload + extracción)

| Amenaza | Riesgo | Control |
|---|---|---|
| **T**ampering — subida de archivo malicioso disfrazado de PDF | Medio-Alto | Validación de tipo MIME real (no solo extensión), tamaño máximo, escaneo con antivirus (ClamAV, protocolo INSTREAM por TCP) antes de persistir en MinIO — implementado en `ConfirmarCargaInformeCommandHandler` (HU-01), único punto que sube archivos a MinIO. **Fail-closed**: si ClamAV no responde (`AntivirusNoDisponibleException`) la carga se rechaza sin persistir nada, en vez de aceptar el archivo sin escanear |
| Denial of Service — PDF corrupto o "PDF bomb" que cuelga el parser | Medio | **Mitigación parcial**: timeout de 30 s por archivo (`PdfParserTimeoutHelper`, `Task.Run` + `CancelAfter`) en `ParsearPdfInformeQueryHandler` (HU-01) y `MigrarInformesCommandHandler` (HU-04) — corta el bloqueo percibido por el usuario/circuito y evita que un archivo colgado tumbe el resto de un lote. **Límite real, no un worker aislado**: el hilo de PdfPig no coopera con cancelación, así que tras el timeout el hilo queda huérfano en el thread pool hasta que el parseo real termine (o nunca, en el peor caso) — no hay proceso/contenedor separado que se pueda matar. Migración masiva además acotada a 40 archivos por corrida (`MigrarInformesCommandValidator.CantidadMaximaArchivos`) por el riesgo de memoria (ver sección de límites de recursos) |
| Injection — contenido del PDF usado sin sanitizar en la UI | Medio | Todo texto extraído se trata como dato, nunca se interpola en HTML sin encode (Blazor ya escapa por defecto — evitar `MarkupString` con contenido no confiable) |

### 4. Infraestructura on-premise

| Amenaza | Riesgo | Control |
|---|---|---|
| **T**ampering — acceso físico/de red no autorizado al servidor | Alto (depende de la institución) | Servidor en red interna, sin exposición directa a Internet; acceso solo vía VPN/LAN institucional; firewall restringiendo puertos (solo 443 hacia la app, resto solo localhost/red interna) |
| **I**nformation Disclosure — tráfico en claro dentro de la LAN | Medio | TLS interno (certificado propio o de la institución) incluso dentro de la LAN — no asumir que "es red interna, no hace falta" |
| Secrets en texto plano (connection strings, claves) | Alto | Variables de entorno vía Docker secrets o `.env` **fuera del control de versiones**; nunca secretos hardcodeados en `appsettings.json` commiteado |
| Falta de backups / pérdida de datos | Alto | Ver `07-plan-despliegue.md` — backups automatizados de PostgreSQL y del bucket MinIO |

## 2FA obligatorio para Supervisor y Admin

```gherkin
Característica: 2FA obligatorio para roles Supervisor y Admin

  Escenario: Admin sin 2FA activado intenta loguearse
    Dado que soy un usuario con rol "Admin" y no tengo 2FA activado
    Cuando ingreso mi email y contraseña correctos
    Entonces se me redirige a configurar 2FA antes de poder acceder a
    cualquier otra página del sistema

  Escenario: Supervisor sin 2FA activado intenta loguearse
    Dado que soy un usuario con rol "Supervisor" y no tengo 2FA activado
    Cuando ingreso mi email y contraseña correctos
    Entonces se me redirige a configurar 2FA antes de poder acceder a
    cualquier otra página del sistema

  Escenario: Analista sin 2FA activado intenta loguearse
    Dado que soy un usuario con rol "Analista" y no tengo 2FA activado
    Cuando ingreso mi email y contraseña correctos
    Entonces accedo normalmente al sistema, sin que se me exija 2FA

  Escenario: Admin completa la configuración obligatoria de 2FA
    Dado que fui redirigido a configurar 2FA de forma obligatoria
    Cuando escaneo el código QR e ingreso el código de verificación válido
    Entonces mi 2FA queda activado y accedo a la página que quería
    visitar originalmente

  Escenario: Admin con 2FA ya activado hace login normal
    Dado que soy Admin y ya tengo 2FA activado
    Cuando ingreso mi email y contraseña correctos
    Entonces se me pide el código de mi aplicación de autenticación
    (flujo actual, sin cambios)

  Escenario: Usuario promovido a Supervisor debe configurar 2FA en su
  próximo login
    Dado que un Administrador me cambia el rol de "Analista" a "Supervisor"
    Y yo no tengo 2FA activado
    Cuando hago login la próxima vez
    Entonces se me exige configurar 2FA antes de continuar
```

### Notas de implementación

- El enforcement corre en `Login.razor` después de `PasswordSignInAsync`
  exitoso, consultando el rol actual del usuario vía `UserManager` — no
  hace falta ningún cambio en `CambiarRolUsuarioCommandHandler`, el
  siguiente login ya lo cubre.
- La página de configuración obligatoria reutiliza el mismo mecanismo de
  `Manage/EnableAuthenticator` (QR + código TOTP), pero en una ruta propia
  sin acceso al resto del sistema hasta completarla.
- No hay período de gracia: la cuenta queda autenticada (cookie creada)
  pero **no puede navegar a ninguna otra página** hasta terminar el setup
  — decisión explícita, más simple que trackear una fecha límite.

## Checklist OWASP Top 10 (mapeo rápido)

| OWASP | Aplica | Mitigación |
|---|---|---|
| A01 Broken Access Control | Sí | RBAC server-side en Application layer, nunca solo en UI |
| A02 Cryptographic Failures | Sí | Argon2id para passwords, TLS en tránsito, MinIO con cifrado en reposo |
| A03 Injection | Sí | EF Core con queries parametrizadas (nunca SQL concatenado); validación de entrada con FluentValidation |
| A04 Insecure Design | Sí | Threat model documentado (este archivo), revisado en cada nueva feature sensible |
| A05 Security Misconfiguration | Sí | Contenedores con usuario no-root, imágenes base mínimas, headers de seguridad (CSP, HSTS) en el reverse proxy |
| A07 Identification and Authentication Failures | Sí | Ver sección 1 |
| A08 Software and Data Integrity Failures | Sí | `.github/workflows/ci.yml` corre `dotnet list package --vulnerable --include-transitive` (JSON parseado con `jq`) y falla el build si aparece algún paquete NuGet con vulnerabilidad conocida, más un escaneo de la imagen Docker final con Trivy (severidad HIGH/CRITICAL, `ignore-unfixed`) — falla el build ante cualquier hallazgo |
| A09 Security Logging and Monitoring Failures | Sí | `AuditLog` + logs estructurados (Serilog) centralizados; alertas ante múltiples intentos de login fallidos |

## Gestión de secretos y CI/CD

- Ningún secreto en el repositorio Git (usar `.gitignore` para `.env`,
  `appsettings.Production.json`).
- Pipeline de CI ejecuta: build, tests, análisis estático (opcional
  SonarQube/Roslyn analyzers), escaneo de dependencias vulnerables y
  escaneo de la imagen Docker antes de permitir el deploy.
- El deploy a producción (servidor on-premise) es manual o vía script
  controlado por el Administrador — no hay pipeline de deploy automático
  hacia el servidor institucional sin aprobación humana, dado que es
  infraestructura crítica de un organismo público.

## Retención y borrado de datos

- Las grabaciones/videos originales de las cámaras **no** se almacenan en
  este sistema (siguen su propio ciclo de 30 días en los sistemas de
  videovigilancia, según se documenta en el pie de cada Informe). El sistema
  solo guarda las **imágenes fijas** ya extraídas como Evidencia.
- No hay por ahora un requisito de borrado automático de Informes/Casos
  (son antecedentes de investigaciones que pueden reabrirse) — se conservan
  indefinidamente salvo instrucción institucional en contrario.
