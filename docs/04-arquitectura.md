# 04 · Arquitectura

## Estilo arquitectónico

**Clean Architecture** con **CQRS liviano** (MediatR) dentro de un **monolito
modular** (no microservicios — ver ADR-003). Blazor Server como frontend
(ver ADR-001).

```
IGE.Informes.Domain          → Entidades, Value Objects, reglas de negocio puras
                                (sin dependencias externas)
IGE.Informes.Application     → Casos de uso (Commands/Queries + Handlers),
                                interfaces de infraestructura (puertos)
IGE.Informes.Infrastructure  → Implementación: EF Core (PostgreSQL), MinIO,
                                parser de PDF (PdfPig), Identity, envío de mail
IGE.Informes.Web             → Blazor Server (UI) + Minimal APIs (si se
                                necesita exponer endpoints a futuro)
```

Regla de dependencia: `Web → Application → Domain`,
`Infrastructure → Application/Domain` (nunca al revés).

## Árbol de carpetas del repositorio

```
proyecto-ige-informes/
├── docs/                                   # Documentación (este conjunto de docs)
│   ├── 00-vision-alcance.md
│   ├── 01-glosario-dominio.md
│   ├── 02-historias-usuario/
│   ├── 03-modelo-dominio.md
│   ├── 04-arquitectura/
│   │   ├── 04-arquitectura.md
│   │   └── adr/                            # Architecture Decision Records
│   ├── 05-api-spec/                        # OpenAPI/contratos (si se exponen endpoints)
│   ├── 06-seguridad-amenazas.md
│   └── 07-plan-despliegue.md
│
├── src/
│   ├── IGE.Informes.Domain/
│   │   ├── Entities/                       # Causa, Informe, Evidencia, Vehiculo, Persona...
│   │   ├── ValueObjects/                   # Dominio (patente), IdRegistro...
│   │   ├── Enums/                          # EstadoVehiculo, RolPersona...
│   │   └── Exceptions/
│   │
│   ├── IGE.Informes.Application/
│   │   ├── Informes/
│   │   │   ├── Commands/                   # CrearInforme, PublicarInforme...
│   │   │   ├── Queries/                    # BuscarInformes, ObtenerFichaVehiculo...
│   │   │   └── Validators/                 # FluentValidation
│   │   ├── Vehiculos/
│   │   ├── Personas/
│   │   ├── Analitica/
│   │   ├── Common/
│   │   │   ├── Interfaces/                 # IPdfParser, IFileStorage, IAuditLogger
│   │   │   └── Behaviors/                  # Pipeline MediatR (logging, validación)
│   │   └── DependencyInjection.cs
│   │
│   ├── IGE.Informes.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── Configurations/             # EF Core Fluent API por entidad
│   │   │   └── Migrations/
│   │   ├── PdfParsing/                     # Extractor por plantilla (PdfPig)
│   │   ├── FileStorage/                    # Cliente MinIO
│   │   ├── Identity/                       # ASP.NET Core Identity + 2FA
│   │   ├── Auditing/                       # SaveChangesInterceptor
│   │   └── DependencyInjection.cs
│   │
│   └── IGE.Informes.Web/
│       ├── Components/
│       │   ├── Pages/                      # Informes, Vehiculos, Personas, Analitica
│       │   └── Shared/
│       ├── Program.cs
│       └── appsettings.json
│
├── tests/
│   ├── IGE.Informes.UnitTests/             # Domain + Application (handlers, validators)
│   └── IGE.Informes.IntegrationTests/      # EF Core con Testcontainers (Postgres real)
│
├── docker/
│   ├── docker-compose.yml                  # app + postgres + minio
│   └── Dockerfile
│
├── IGE.Informes.sln
└── README.md
```

## Componentes clave

| Componente | Tecnología | Rol |
|---|---|---|
| Base de datos | PostgreSQL | Persistencia relacional + full-text search (tsvector/GIN) |
| Almacenamiento de archivos | MinIO (S3-compatible) | PDFs originales, imágenes de evidencias/vehículos/personas |
| Extracción de PDF | PdfPig + parser por plantilla | Extrae texto y lo mapea a campos estructurados |
| Autenticación | ASP.NET Core Identity | Login propio, 2FA (TOTP), lockout |
| Autorización | Policy-based (roles: Analista/Supervisor/Admin) | RBAC |
| Auditoría | EF Core `SaveChangesInterceptor` + middleware de lectura | Registro de accesos y cambios |
| Background jobs | Hangfire (o `IHostedService` + canal en memoria) | Procesamiento asíncrono de extracción de PDFs y migración masiva |
| Contenerización | Docker Compose | app + postgres + minio en un único host |

Ver el detalle y las alternativas descartadas de cada decisión en
`docs/04-arquitectura/adr/`.
