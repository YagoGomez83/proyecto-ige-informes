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

## Fase 5 · Hardening de seguridad y despliegue final

Referencia: `docs/06-seguridad-amenazas.md`, `docs/07-plan-despliegue.md`

- [ ] Revisión de checklist OWASP Top 10 completo.
- [ ] Rate limiting y lockout de login verificados con test de carga básico.
- [ ] Backups automatizados configurados y **probados con una restauración
      real** antes de considerar la fase cerrada.
- [ ] TLS interno configurado en el reverse proxy.
- [ ] Documentación de onboarding/offboarding de usuarios para el
      Administrador.

**Criterio de cierre**: simulacro de restauración completa desde backup
exitoso, y checklist de seguridad firmado por el equipo antes de habilitar
el sistema para todos los usuarios.

---

## Cómo trabajar cada fase con Claude Code

1. Abrir una sesión nueva por fase (o por HU si la fase es grande).
2. Pedir explícitamente: *"Leé `CLAUDE.md` y la HU-XX antes de tocar
   código"* si la sesión no lo hace por defecto.
3. Pedir tests antes o junto con la implementación, no después.
4. Al cerrar la fase, actualizar el checklist de este documento (tildar los
   ítems) como parte del commit — este archivo es el tablero de avance real
   del proyecto.
