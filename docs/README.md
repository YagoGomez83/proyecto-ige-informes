# IGE Informes — Sistema de Gestión de Informes de Análisis de Videovigilancia

Sistema de gestión documental, indexación y analítica para los Informes
Especiales de Análisis de Cámaras de Videovigilancia del Instituto de
Gestión de Emergencias 4.0 (San Luis).

## Documentación

Empezá por acá, en orden:

1. [`docs/00-vision-alcance.md`](docs/00-vision-alcance.md) — qué problema resolvemos y qué queda fuera de alcance
2. [`docs/01-glosario-dominio.md`](docs/01-glosario-dominio.md) — lenguaje ubicuo del dominio
3. [`docs/02-historias-usuario/`](docs/02-historias-usuario/) — épicas e historias con criterios de aceptación (Gherkin)
4. [`docs/03-modelo-dominio.md`](docs/03-modelo-dominio.md) — entidades, relaciones e invariantes
5. [`docs/04-arquitectura/`](docs/04-arquitectura/) — decisiones de arquitectura (ADRs) y árbol de carpetas de la solución
6. `docs/06-seguridad-amenazas.md` — threat model (pendiente)
7. `docs/07-plan-despliegue.md` — plan de despliegue on-premise (pendiente)

## Stack

- **Backend**: ASP.NET Core (C#), Clean Architecture + CQRS liviano (MediatR)
- **Frontend**: Blazor Server
- **Base de datos**: PostgreSQL
- **Almacenamiento de archivos**: MinIO (S3-compatible, self-hosted)
- **Infraestructura**: Docker Compose (on-premise)

## Estado del proyecto

🚧 En fase de documentación / diseño. Ver `docs/` para el detalle completo
antes de generar el código con Claude Code.
