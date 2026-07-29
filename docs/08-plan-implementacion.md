# 08 · Plan de Implementación (para ejecutar con Claude Code)

> Metodología: Spec-Driven Development. Cada fase se cierra cuando el código
> cumple los criterios de aceptación Gherkin de las HU involucradas — no
> antes. No se avanza a la fase siguiente con deuda de la anterior sin
> registrarla explícitamente.

## Fase 0 · Scaffolding de la solución — ✅ CERRADA

**Objetivo**: tener el esqueleto de la solución corriendo en Docker Compose,
sin lógica de negocio todavía, pero con la base de Identity y auditoría lista.

- [x] Crear `.sln` con los 4 proyectos (`Domain`, `Application`,
      `Infrastructure`, `Web`) respetando las dependencias de
      `04-arquitectura.md`.
- [x] Configurar `AppDbContext` (EF Core) apuntando a PostgreSQL, con la
      primera migración vacía.
- [x] Configurar ASP.NET Core Identity (usuarios, roles: Analista/
      Supervisor/Admin) + 2FA (TOTP).
- [x] Implementar `AuditLogInterceptor` genérico (EF Core
      `SaveChangesInterceptor`) — antes de cualquier entidad de negocio,
      para que nazca integrado desde el primer commit.
- [x] `docker-compose.yml` con `web`, `postgres`, `minio`, `reverse-proxy`
      (ver `07-plan-despliegue.md`).
- [x] Blazor Server arrancando con login/logout funcional contra Identity.

**Criterio de cierre**: `docker compose up` levanta todo, un usuario Admin
puede loguearse con 2FA, y queda un registro en `AuditLog` del login.
**Cumplido y verificado** — security-reviewer sin bloqueantes (ver memoria
del proyecto). Deuda diferida explícitamente: rate limiting de login
(Fase 5) y 2FA obligatorio para Supervisor/Admin (a decidir).

---

## Fase 1 · Núcleo: Casos de Análisis (Épica 00) — ✅ CERRADA

Referencia: `docs/epic-00-gestion-casos-analisis.md`

- [x] Entidades `CasoAnalisis`, `TipoIncidente`, `Dependencia`,
      `CasoAnalista` en `Domain`.
- [x] HU-00: alta de Caso (Command + validación + UI Blazor).
- [x] HU-01: cambio de estado/resultado del Caso.
- [x] HU-02: vinculación de Vehículo/Persona/Cámara al Caso (con soporte de
      texto libre cuando no hay ficha de catálogo todavía — ver invariantes
      del modelo de dominio). Implementado como texto libre por decisión
      explícita (la vinculación a catálogo real de Vehiculo/Persona se
      dio en Fase 2, sin volver a tocar este flujo).
- [~] Catálogo básico de `TipoIncidente` — el modelo y el alta manual vía
      UI existen; falta la carga real de todos los códigos relevados del
      Excel histórico (164, 162, 02, 25, etc.), solo se cargaron un par a
      mano en pruebas. **Deuda pendiente, no bloqueante.**
- [x] (Adelantado desde Fase 3, por pedido explícito) HU-03 de esta épica:
      generar `Informe` (Borrador) desde un `CasoAnalisis`, con `Causa` y
      `Dependencia` destino — sin parser de PDF ni carga de archivo, eso
      sigue siendo parte de Fase 3.
- [x] Pipeline de autorización RBAC de MediatR (`AutorizacionBehavior`,
      fail-closed) y `ValidationBehavior` — no estaban en el plan original,
      se agregaron como bloqueante de seguridad antes del primer Command.

**Criterio de cierre**: un Analista puede reemplazar completamente su fila
de Excel diaria por un Caso cargado en el sistema, en igual o menor tiempo.
**Cumplido y verificado end-to-end en navegador.**

---

## Fase 2 · Catálogos: Vehículos, Personas, Cámaras (Épica 03) — ✅ CERRADA

Referencia: `docs/epic-03-gestion-vehiculos-personas.md`

- [x] Entidades `Vehiculo`, `CategoriaAlerta`, `Persona`, `Camara`.
- [x] HU-08: alta/edición de Vehículo, con categorías de alerta múltiples,
      acción a realizar, avisar a, fecha de baja.
- [x] HU-09: alta/edición de Persona (identificada o no).
- [x] HU-10: catálogo de Cámaras — CRUD manual completo (alta/completar
      ubicación/cambiar tipo, restringido a rol Admin; lectura abierta a
      los 3 roles). El escenario de autocompletado automático desde el
      extractor de PDF queda diferido a Fase 3 (no existe el extractor
      todavía).
- [x] **Migración** del catálogo de vehículos desde
      `Relevamiento Dominios cargados Hik Central.xlsx` — herramienta de
      consola dedicada (`src/IGE.Informes.DataMigration`), corrida contra
      la base real: 1110 Vehiculos consolidados por Dominio, 4
      CategoriaAlerta reales asignadas (Robado/Narcotrafico/Inhibidores/
      RoboCubiertas). Deduplicación por Dominio normalizado; solo 4 de las
      13 hojas del Excel tenían filas de datos reales (confirmado por
      inspección directa, no por el resumen inicial que resultó
      incorrecto — ver memoria del proyecto).

**Criterio de cierre**: el catálogo de vehículos migrado es consultable y
editable, sin duplicados, y con las categorías de alerta correctamente
etiquetadas. **Cumplido y verificado end-to-end en navegador** (1110
Vehiculos visibles y editables en `/vehiculos`).

---

## Fase 3 · Informes y extracción de PDF (Épica 01) — ✅ CERRADA

Referencia: `docs/epic-01-gestion-informes.md`

- [x] Entidades `Informe`, `Causa`, `InformeAnalista`, `Evidencia`.
- [x] Parser de PDF por plantilla (PdfPig + patrones) — ver ADR-004.
      **Deuda pendiente, no bloqueante**: nunca se consiguieron los 3 PDFs
      de muestra reales — el parser solo está validado contra PDFs
      sintéticos generados en los tests con la misma estructura.
- [x] HU-03 (épica 00): generar Informe desde un Caso existente, a pedido
      de una Dependencia. **Adelantada y cerrada en Fase 1** (Command
      `GenerarInformeDesdeCasoCommand`, `IdRegistro` autogenerado
      correlativo por año).
- [x] HU-01 (épica 01): carga de Informe desde PDF con extracción y
      revisión manual antes de guardar, subida real a MinIO (commit
      `fe7203b`).
- [x] HU-02 (épica 01): editar/corregir metadatos de un Informe en
      Borrador — Relato, Dependencia destino, Causa (commit `f836ae3`).
- [x] HU-03 (épica 01): publicar/firmar un Informe — un click agrega al
      usuario actual como Firmante y publica (commit `ad1ca3d`).
- [x] HU-04 (épica 01): migración masiva de PDFs históricos desde una
      carpeta local (subida múltiple vía Web, no integración directa con
      Drive) — requirió relajar `Informe.CasoAnalisisId` a nullable con
      un campo `Origen` nuevo, ver `03-modelo-dominio.md` (commit
      `a0ced17`).

**Criterio de cierre**: cumplido con PDFs sintéticos (misma estructura que
los reales, ver skill `pdf-informe-parser`) — genera automáticamente un
Informe con Causa, Evidencias y vehículos/personas extraídos. **No
verificado contra los 3 PDFs reales de muestra** (nunca se consiguieron
en el repo) — si aparecen, correr el parser contra ellos antes de dar la
deuda por saldada.

---

## Fase 4 · Búsqueda y Analítica (Épica 02)

Referencia: `docs/02-historias-usuario/epic-02-busqueda-analitica.md`

- [x] Full-text search sobre `Informe.relato` (PostgreSQL `tsvector`,
      ADR-002) — usado por HU-05. `CasoAnalisis.observaciones` no tiene
      full-text propio: HU-06 no lo necesitó (solo agrupa/cuenta, no busca
      texto libre) y HU-05 ya cubre búsqueda de Informes; queda como deuda
      si en el futuro se pide buscar texto libre sobre Casos.
- [x] HU-05: búsqueda combinada (dependencia, código de incidente, dominio,
      DNI/nombre, texto libre, rango de fechas). Commit af090d8.
- [x] HU-06: tablero de analítica (casos por dependencia, tipo de
      incidente, resultado, analista — ver nota de alcance en
      `docs/epic-02-busqueda-analitica.md`, cuenta CasoAnalisis no
      Informe) + exportación a CSV. Commit e35a592.
- [x] HU-07: ficha 360° de Vehículo/Persona (historial de Informes en las
      páginas de detalle existentes). Commit 1deb312.

**Criterio de cierre**: el Supervisor genera el reporte trimestral de
"casos por dependencia y resultado" sin abrir Excel.

---

## Fase 5 · Hardening de seguridad y despliegue final — ✅ CERRADA

Referencia: `docs/06-seguridad-amenazas.md`, `docs/07-plan-despliegue.md`

- [x] Revisión de checklist OWASP Top 10 completo — verificado ítem por
      ítem contra el código real (no solo contra la documentación) vía
      `security-reviewer`. A01/A02/A03/A05/A09 cumplen. A04 y A07 tenían
      documentación desactualizada/hallazgos, corregidos en esta fase (ver
      abajo). A08 queda como deuda explícita (no bloqueante).
- [x] Rate limiting y lockout de login verificados. Lockout de Identity
      (5 intentos, 15 min) ya existía desde Fase 0. Rate limiting nuevo:
      `Microsoft.AspNetCore.RateLimiting`, 10 req/min por IP, acotado a
      `/Account` (Blazor Server no permite atar una política a un
      componente Razor individual, así que se usa `GlobalLimiter` +
      `UseWhen`). Verificado con test de carga básico (`curl` en loop):
      request 11 en la misma ventana devuelve 429, otras rutas no se ven
      afectadas. **Hallazgo del security-reviewer corregido en la misma
      fase**: faltaba `ForwardedHeaders` — sin él, el limiter particionaba
      por la IP interna de Caddy en vez de la del cliente real, compartiendo
      el límite entre todos los usuarios. Agregado `UseForwardedHeaders` +
      verificado con dos IPs simuladas vía `X-Forwarded-For` (una se
      bloquea, la otra no se ve afectada).
- [x] Backups automatizados configurados y **probados con una restauración
      real**: `docker/backup/` (scripts `backup-postgres.sh`,
      `backup-minio.sh`, `restore-postgres.sh` + sidecar con cron diario a
      las 03:00, activable con `docker-compose.backup.yml`). Simulacro de
      restauración ejecutado contra una base de prueba: los conteos de
      filas post-restauración coinciden exactamente con el origen (854
      Cámaras, 80 Dependencias, 54 Localidades, 1 Informe); los 5 archivos
      de MinIO espejados verificados íntegros. Detalle en
      `07-plan-despliegue.md`, sección "Implementación (Fase 5)".
- [x] TLS interno configurado en el reverse proxy — ya existía desde
      Fase 0 (`docker/Caddyfile`, `tls internal` + HSTS/CSP/X-Frame-Options),
      confirmado sin downgrade posible (no hay bloque HTTP alternativo).
- [x] Documentación de onboarding/offboarding de usuarios:
      `docs/09-onboarding-offboarding-usuarios.md`. Registra como hallazgo
      que **no existe todavía una UI de gestión de usuarios** — el alta/baja
      real hoy es manual (script de consola o directo contra Postgres,
      nunca borrando la cuenta en la baja por la FK de `AuditLog`).

**Criterio de cierre**: simulacro de restauración completa desde backup
exitoso — **cumplido**. Checklist de seguridad revisado con hallazgos
corregidos donde aplicaba — **cumplido**, con deuda explícita registrada
abajo (no bloqueante para habilitar el sistema a los primeros usuarios,
sí antes de un despliegue a producción sin supervisión).

**Deuda registrada (no bloqueante)**:
- ~~A08 OWASP: falta escaneo de imagen Docker (Trivy).~~ **Resuelto**: el CI
  (`.github/workflows/ci.yml`) corre `dotnet list package --vulnerable
  --include-transitive` (paquetes NuGet) y además construye la imagen
  Docker de `web` y la escanea con Trivy (severidad HIGH/CRITICAL,
  `ignore-unfixed`), fallando el build ante cualquier hallazgo de
  cualquiera de los dos escaneos.
- ~~2FA (TOTP) es autoservicio, no obligatorio para ningún rol.~~
  **Resuelto** (commit `0062441`): 2FA obligatorio para Admin/Supervisor,
  sin período de gracia — ver `docs/06-seguridad-amenazas.md`, sección 1.
  Esta entrada había quedado desactualizada tras la implementación real.
- No hay UI de gestión de usuarios (alta/cambio de rol/bloqueo) — todo el
  proceso de `09-onboarding-offboarding-usuarios.md` es manual.
  **(Resuelto en HU-17.)**
- ~~No hay invalidación activa de sesión al hacer offboarding de un usuario
  (solo bloqueo de logins futuros).~~ **Resuelto**: `BloquearAsync`/
  `CambiarRolAsync` invalidan el `SecurityStamp`, que
  `PersistingRevalidatingAuthenticationStateProvider` detecta en su próximo
  ciclo de revalidación (hasta 30 min de latencia, ver
  `docs/09-onboarding-offboarding-usuarios.md`).
- El volumen de backups (`docker-compose.backup.yml`) es local en el
  entorno de desarrollo/simulacro — en producción debe montarse sobre
  almacenamiento externo al servidor antes de confiar en él.

---

## Fase 6 · Gestión de Catálogos (Épica 04)

Referencia: `docs/epic-04-gestion-catalogos.md`

Agregada después del cierre de la Fase 4 — hasta ahora `Dependencia` y
`Camara` existían en `Domain` sin ningún alta manual desde la UI (los
datos se cargaban por fuera del código). Esta fase agrega el CRUD que
faltaba, más el modelo de jurisdicción geográfica (`Barrio`) pedido para
poder asociar Cámaras y Casos a una zona real.

- [x] Entidad `Barrio` en `Domain`, con catálogo administrable.
- [x] `Dependencia` extendida: colección de `Barrio` (jurisdicción
      geográfica opcional, sin restricción por `Tipo`), `Nombre` único.
- [x] `Camara` extendida: `DependenciaId` opcional (nullable).
- [x] HU-11: alta de Dependencias + asignación de Barrios de jurisdicción.
- [x] HU-12: alta manual de Cámaras con Dependencia opcional.
- [x] HU-13: catálogo de Barrios.
- [x] Rutas nuevas bajo `/configuracion` (liberado el placeholder de
      `Proximamente.razor`), restringidas a rol Admin (páginas de alta y
      acciones de edición de Cámara envueltas en `AuthorizeView
      Roles="Admin"`, hallazgo del `security-reviewer` sobre
      `Camaras/Detalle.razor` corregido antes de cerrar).

**Criterio de cierre**: el Administrador da de alta una Dependencia nueva,
le asigna Barrios de jurisdicción, y da de alta una Cámara vinculada a esa
Dependencia, todo desde la UI sin tocar la base de datos directamente.
**Cumplido y verificado en navegador** (flujo completo: Barrio → Dependencia
→ asignación → Cámara con y sin Dependencia, confirmado en base real con
`AuditLog` correcto).

**Deuda registrada (no bloqueante)**: la migración `AddBarrioYJurisdiccion`
crea el índice único `IX_Dependencias_Nombre` sin un paso previo de
detección/consolidación de duplicados — señalado por el `security-reviewer`.
No aplica en el entorno actual (sin duplicados verificado). **Confirmado
que tampoco aplica al despliegue en la VM de producción**: arranca con una
base vacía, corriendo todas las migraciones en orden desde cero, sin
ningún dato preexistente de `Dependencia` cargado por fuera de EF Core —
si en el futuro se planea importar/restaurar Dependencias de otro sistema
antes de correr la app, revisar este punto entonces.

---

## Cómo trabajar cada fase con Claude Code

1. Abrir una sesión nueva por fase (o por HU si la fase es grande).
2. Pedir explícitamente: *"Leé `CLAUDE.md` y la HU-XX antes de tocar
   código"* si la sesión no lo hace por defecto.
3. Pedir tests antes o junto con la implementación, no después.
4. Al cerrar la fase, actualizar el checklist de este documento (tildar los
   ítems) como parte del commit — este archivo es el tablero de avance real
   del proyecto.
