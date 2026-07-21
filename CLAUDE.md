# CLAUDE.md — Instrucciones para Claude Code en este repositorio

Este archivo se lee automáticamente al iniciar una sesión de Claude Code en
este repo. Contiene las reglas que **siempre** hay que respetar.

## Antes de escribir código

**Nunca implementes una historia de usuario sin haber leído antes:**
1. `docs/01-glosario-dominio.md` — los nombres de clases deben coincidir
   exactamente con los términos del glosario (`CasoAnalisis`, no `Caso` ni
   `AnalysisCase`).
2. `docs/03-modelo-dominio.md` — entidades, relaciones e invariantes.
3. La historia de usuario específica en `docs/epic-00-gestion-casos-analisis.md`,
   `docs/epic-01-gestion-informes.md`, `docs/epic-02-busqueda-analitica.md`
   o `docs/epic-03-gestion-vehiculos-personas.md` según la épica — los
   criterios de aceptación en Gherkin son la definición de "terminado".
4. Los ADRs relevantes (`docs/ADR-*.md`) antes de introducir una librería
   o patrón nuevo — si ya hay una decisión tomada, respetarla; si creés
   que hay que cambiarla, proponelo explícitamente en vez de ignorarla en
   silencio.

Si una historia de usuario es ambigua o le falta un dato para implementarla
correctamente, **preguntame antes de asumir** — no improvises reglas de
negocio nuevas.

## Reglas de arquitectura (no negociables)

- **Clean Architecture**: `Domain` no depende de nada. `Application` depende
  solo de `Domain`. `Infrastructure` y `Web` dependen de `Application`/
  `Domain`, nunca al revés.
- Cada caso de uso es un `Command` o `Query` + su `Handler` (MediatR) en
  `Application`, con su `Validator` (FluentValidation) al lado.
- El `Domain` no conoce Entity Framework — nada de atributos de EF Core en
  las entidades de dominio; el mapeo va en `Infrastructure/Persistence/
  Configurations` (Fluent API).
- Toda entidad o value object nuevo debe agregarse primero al glosario
  (`01-glosario-dominio.md`) si no está — el código y la documentación no
  se desincronizan.

## Seguridad (no negociable, ver `docs/06-seguridad-amenazas.md`)

- Toda query/command que lea o modifique `Informe`, `CasoAnalisis`,
  `Vehiculo` o `Persona` debe registrar el evento en `AuditLog` — no es
  opcional, es un requisito de cada Handler, no una feature aparte.
- Nunca loguear DNI, contraseñas ni contenido de relatos en logs de texto
  plano (Serilog) — solo IDs y nombres de entidad.
- Autorización siempre server-side (policy de MediatR pipeline behavior),
  nunca confiar en lo que el cliente Blazor oculta u oculta en la UI.
- Ningún secreto (connection string, claves) hardcodeado ni commiteado —
  usar `appsettings.Development.json` (gitignored) o variables de entorno.

## Testing

- Toda regla de negocio en `Domain` y todo `Handler` en `Application` lleva
  test unitario (`tests/IGE.Informes.UnitTests`).
- Los criterios de aceptación en Gherkin de cada HU son la base para los
  tests — si un escenario Gherkin no tiene test equivalente, la historia
  no está terminada.
- Los tests de integración (`tests/IGE.Informes.IntegrationTests`) usan
  Testcontainers con PostgreSQL real — no mockear la base de datos en tests
  de integración.

## Convenciones de código

- C# con nullable reference types habilitado (`<Nullable>enable</Nullable>`).
- Nombrado en español para conceptos de dominio (`CasoAnalisis`,
  `PublicarInformeCommand`), inglés para infraestructura técnica genérica
  (`IFileStorage`, `AuditLogInterceptor`) — mantiene consistencia con el
  glosario sin forzar traducciones raras de términos técnicos.
- Commits pequeños y descriptivos, uno por Historia de Usuario o por tarea
  técnica clara (no mezclar features distintas en un commit).

## Qué NO hacer

- No generar el PDF del Informe — el sistema solo lo indexa (ver
  `00-vision-alcance.md`, sección "Fuera de alcance").
- No implementar reconocimiento facial ni matching de imágenes por
  similitud — catálogo + búsqueda manual alcanza (ver ADR correspondiente
  si se agrega en el futuro).
- No migrar el histórico de Casos de Análisis del Excel — arrancan desde
  cero (ver `00-vision-alcance.md`).
- No agregar Kubernetes, Elasticsearch, microservicios ni otra pieza de
  infraestructura no listada en los ADRs sin proponerlo primero — evitar
  sobre-ingeniería es un principio explícito de este proyecto.

## Skills y subagentes disponibles en este repo

- **Skill `pdf-informe-parser`** (`.claude/skills/pdf-informe-parser/`):
  reglas concretas de extracción de la plantilla de Informe, sacadas de los
  3 PDFs de muestra analizados. Consultar antes de tocar
  `IGE.Informes.Infrastructure/PdfParsing`.
- **Skill `nueva-entidad-auditada`** (`.claude/skills/nueva-entidad-auditada/`):
  checklist paso a paso para agregar cualquier entidad nueva del dominio
  respetando Clean Architecture y el requisito de auditoría. Usar siempre
  que se implemente una entidad nueva (Fases 1, 2 y 3 del plan).
- **Subagente `security-reviewer`** (`.claude/agents/security-reviewer.md`):
  invocarlo al cerrar cada fase del plan de implementación, o al tocar
  autenticación/autorización/archivos/datos personales. Solo lee, nunca
  escribe código.
- **Subagente `gherkin-test-writer`** (`.claude/agents/gherkin-test-writer.md`):
  invocarlo al arrancar cualquier HU nueva, para generar los tests a partir
  de los escenarios Gherkin **antes** de la implementación.
- **Skill `ige-design-system`** (`.claude/skills/ige-design-system/`):
  tokens de color/tipografía/espaciado y componentes compartidos (Card,
  StatusChip, botones, inputs) para toda la UI de Blazor. Consultar
  siempre antes de crear o editar un componente en
  `IGE.Informes.Web/Components` — especialmente la tabla de mapeo de
  estados del dominio a color semántico (sección 3 del skill), para no
  reinventar esa decisión en cada página. Ver ADR-005.

No se crean subagentes separados por capa (frontend/backend): con Clean
Architecture ya separada en carpetas y desarrollo secuencial por fases, esa
separación no aporta valor y solo multiplica el consumo de tokens.

## Orden de implementación

Seguir `docs/08-plan-implementacion.md` — no saltar de fase salvo que el
usuario lo pida explícitamente.
