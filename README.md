# IGE Informes — Sistema de Gestión de Informes de Análisis de Videovigilancia

Sistema de gestión documental, indexación y analítica para los Informes
Especiales de Análisis de Cámaras de Videovigilancia del Instituto de
Gestión de Emergencias 4.0 (San Luis).

## Documentación

Empezá por acá, en orden:

1. [`docs/00-vision-alcance.md`](docs/00-vision-alcance.md) — qué problema resolvemos y qué queda fuera de alcance
2. [`docs/01-glosario-dominio.md`](docs/01-glosario-dominio.md) — lenguaje ubicuo del dominio
3. Épicas e historias con criterios de aceptación (Gherkin): [`docs/epic-00-gestion-casos-analisis.md`](docs/epic-00-gestion-casos-analisis.md) (núcleo real del sistema, reemplaza el Excel diario), [`docs/epic-01-gestion-informes.md`](docs/epic-01-gestion-informes.md), [`docs/epic-02-busqueda-analitica.md`](docs/epic-02-busqueda-analitica.md), [`docs/epic-03-gestion-vehiculos-personas.md`](docs/epic-03-gestion-vehiculos-personas.md)
4. [`docs/03-modelo-dominio.md`](docs/03-modelo-dominio.md) — entidades, relaciones e invariantes
5. [`docs/04-arquitectura.md`](docs/04-arquitectura.md) y los ADRs (`docs/ADR-*.md`) — decisiones de arquitectura y árbol de carpetas de la solución
6. [`docs/06-seguridad-amenazas.md`](docs/06-seguridad-amenazas.md) — threat model (STRIDE + OWASP)
7. [`docs/07-plan-despliegue.md`](docs/07-plan-despliegue.md) — plan de despliegue on-premise
8. [`docs/08-plan-implementacion.md`](docs/08-plan-implementacion.md) — plan de implementación por fases, con el estado real de avance

## Para trabajar con Claude Code

Este repo incluye [`CLAUDE.md`](CLAUDE.md) con las reglas persistentes que
Claude Code debe respetar en cada sesión (arquitectura, seguridad, testing,
convenciones). Se lee automáticamente al abrir el repo.

## Stack

- **Backend**: ASP.NET Core (C#), Clean Architecture + CQRS liviano (MediatR)
- **Frontend**: Blazor Server
- **Base de datos**: PostgreSQL
- **Almacenamiento de archivos**: MinIO (S3-compatible, self-hosted)
- **Infraestructura**: Docker Compose (on-premise)

## Estado del proyecto

✅ Fases 0, 1 y 2 completas y verificadas (scaffolding + Identity + auditoría,
Casos de Análisis, y catálogos de Vehículos/Personas/Cámaras con la
migración real del histórico de vehículos). En curso: Fase 3 (Informes y
extracción de PDF). Ver [`docs/08-plan-implementacion.md`](docs/08-plan-implementacion.md)
para el detalle de avance por fase.

### Cómo levantar el entorno de desarrollo

```bash
cd docker
cp .env.example .env   # completar los valores CHANGE_ME
docker compose up -d --build
```

La app queda disponible en `https://localhost` (TLS interno autofirmado)
o en `http://localhost:8443` (HTTP directo al contenedor `web`, solo para
desarrollo — ver `docker/docker-compose.override.yml`).

### Cómo correr los tests

```bash
dotnet test tests/IGE.Informes.UnitTests
dotnet test tests/IGE.Informes.IntegrationTests   # requiere Docker (Testcontainers)
```
