---
description: Playbook paso a paso para agregar una entidad de dominio nueva (ej. CasoAnalisis, Vehiculo, Persona) respetando Clean Architecture, el patrón CQRS del proyecto y el requisito no negociable de auditoría. Usar cada vez que se implemente una entidad nueva del modelo de dominio.
---

# Agregar una entidad de dominio nueva — checklist

Seguir este orden exacto, no saltear pasos:

## 1. Domain
- Crear la entidad en `IGE.Informes.Domain/Entities/` — **sin** atributos de
  EF Core, sin dependencias externas. Solo propiedades, invariantes propias
  (validadas en el constructor o en métodos de la entidad) y, si aplica,
  Value Objects en `Domain/ValueObjects/`.
- Verificar que el nombre y los campos coincidan exactamente con
  `docs/01-glosario-dominio.md` y `docs/03-modelo-dominio.md`. Si falta algo
  en esos documentos, actualizarlos primero — no improvisar campos nuevos
  sin reflejarlos en la spec.

## 2. Application
- Un `Command` (alta/edición) y sus `Query` (detalle/listado) en
  `Application/<NombreEntidad>/Commands` y `.../Queries`.
- Un `Validator` (FluentValidation) al lado de cada Command.
- El Handler del Command de alta/edición debe, sin excepción, encadenar el
  registro en `AuditLog` (ver paso 4) — no como una feature aparte, como
  parte del mismo Handler o vía pipeline behavior de MediatR.

## 3. Infrastructure
- `IEntityTypeConfiguration<NombreEntidad>` en
  `Infrastructure/Persistence/Configurations/` (Fluent API, nunca Data
  Annotations en el Domain).
- Migración de EF Core (`dotnet ef migrations add Add<NombreEntidad>`).
- Si la entidad tiene campos de "características libres" (ver Vehículo,
  Persona en el glosario), mapear como columna `jsonb`, no como tabla de
  atributos dinámica.

## 4. Auditoría (no negociable, ver docs/06-seguridad-amenazas.md)
- Confirmar que el `AuditLogInterceptor` genérico ya captura los cambios de
  esta entidad automáticamente (si el interceptor está bien hecho, no
  debería requerir código adicional por entidad para el registro de
  escritura).
- Para lecturas (Queries de detalle), agregar explícitamente el logging de
  acceso en el Handler — esto sí requiere código por Query, no lo cubre el
  interceptor de escritura.

## 5. Web (Blazor)
- Componente de listado + componente de alta/edición en
  `Web/Components/Pages/<NombreEntidad>/`.
- Validación client-side espejo de FluentValidation, pero **nunca** confiar
  solo en ella — el Handler revalida siempre server-side.

## 6. Tests
- Test unitario de las invariantes del Domain.
- Test unitario del Handler (con repositorio en memoria o mockeado).
- Al menos un test de integración con Testcontainers que confirme que el
  `AuditLog` efectivamente registra la operación.

## 7. Cerrar el círculo con la documentación
- Si esta entidad cambia algo respecto a lo documentado (un campo nuevo,
  una regla distinta), actualizar `03-modelo-dominio.md` en el mismo commit
  — la spec y el código no se desincronizan.
