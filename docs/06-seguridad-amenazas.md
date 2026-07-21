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
| **S**poofing — suplantación de identidad | Alto (sin AD/LDAP, credenciales propias) | Hash de contraseñas con **Argon2id**; política de contraseña mínima (12+ caracteres); **2FA (TOTP)** obligatorio para roles Supervisor/Admin, recomendado para Analista |
| **S**poofing — fuerza bruta / credential stuffing | Alto | Bloqueo de cuenta tras 5 intentos fallidos (lockout progresivo); rate limiting en el endpoint de login |
| **E**levation of Privilege | Medio | RBAC estricto por policy (`Analista` / `Supervisor` / `Admin`) validado en el backend en cada Command/Query — nunca confiar en el rol mostrado en el cliente |
| **R**epudiation | Medio | Toda acción de login/logout queda en `AuditLog` con IP y user-agent |

### 2. Acceso a datos (Informes, Casos, Vehículos, Personas)

| Amenaza | Riesgo | Control |
|---|---|---|
| **I**nformation Disclosure — acceso no autorizado a un Informe/Persona | Alto | Autorización a nivel de Application layer (no solo UI); todo acceso de lectura a `Informe`, `CasoAnalisis`, `Vehiculo`, `Persona` se registra en `AuditLog` (usuario, entidad, timestamp) — es requisito, no opcional |
| **T**ampering — modificación no autorizada de un Informe publicado | Alto | Un `Informe` en estado `Publicado` es inmutable salvo por un flujo explícito de "corrección" que genera nueva versión y conserva el historial (nunca sobreescribe) |
| **R**epudiation — negar haber modificado/consultado algo | Medio | `AuditLog` append-only, sin permisos de borrado ni para Admin desde la aplicación |
| **I**nformation Disclosure — archivos (PDFs/imágenes) accesibles por URL directa | Alto | MinIO con **URLs prefirmadas de corta expiración** (nunca URLs públicas permanentes); bucket privado por defecto |

### 3. Ingesta de PDFs (upload + extracción)

| Amenaza | Riesgo | Control |
|---|---|---|
| **T**ampering — subida de archivo malicioso disfrazado de PDF | Medio-Alto | Validación de tipo MIME real (no solo extensión), tamaño máximo, escaneo con antivirus (ClamAV) antes de persistir en MinIO |
| Denial of Service — PDF corrupto o "PDF bomb" que cuelga el parser | Medio | Timeout y límite de memoria en el proceso de extracción (background worker aislado, no en el hilo de la request) |
| Injection — contenido del PDF usado sin sanitizar en la UI | Medio | Todo texto extraído se trata como dato, nunca se interpola en HTML sin encode (Blazor ya escapa por defecto — evitar `MarkupString` con contenido no confiable) |

### 4. Infraestructura on-premise

| Amenaza | Riesgo | Control |
|---|---|---|
| **T**ampering — acceso físico/de red no autorizado al servidor | Alto (depende de la institución) | Servidor en red interna, sin exposición directa a Internet; acceso solo vía VPN/LAN institucional; firewall restringiendo puertos (solo 443 hacia la app, resto solo localhost/red interna) |
| **I**nformation Disclosure — tráfico en claro dentro de la LAN | Medio | TLS interno (certificado propio o de la institución) incluso dentro de la LAN — no asumir que "es red interna, no hace falta" |
| Secrets en texto plano (connection strings, claves) | Alto | Variables de entorno vía Docker secrets o `.env` **fuera del control de versiones**; nunca secretos hardcodeados en `appsettings.json` commiteado |
| Falta de backups / pérdida de datos | Alto | Ver `07-plan-despliegue.md` — backups automatizados de PostgreSQL y del bucket MinIO |

## Checklist OWASP Top 10 (mapeo rápido)

| OWASP | Aplica | Mitigación |
|---|---|---|
| A01 Broken Access Control | Sí | RBAC server-side en Application layer, nunca solo en UI |
| A02 Cryptographic Failures | Sí | Argon2id para passwords, TLS en tránsito, MinIO con cifrado en reposo |
| A03 Injection | Sí | EF Core con queries parametrizadas (nunca SQL concatenado); validación de entrada con FluentValidation |
| A04 Insecure Design | Sí | Threat model documentado (este archivo), revisado en cada nueva feature sensible |
| A05 Security Misconfiguration | Sí | Contenedores con usuario no-root, imágenes base mínimas, headers de seguridad (CSP, HSTS) en el reverse proxy |
| A07 Identification and Authentication Failures | Sí | Ver sección 1 |
| A08 Software and Data Integrity Failures | Sí | Dependencias con `dotnet list package --vulnerable` en CI; imágenes Docker escaneadas (Trivy) antes de desplegar |
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
