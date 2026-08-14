# Épica 03 · Gestión de Vehículos y Personas

## HU-08 · Alta/edición manual de vehículo

**Como** Analista
**Quiero** dar de alta o editar un vehículo manualmente (fuera de la carga de un informe)
**Para** mantener actualizado su estado de investigación

```gherkin
Característica: Gestión de vehículos

  Escenario: Cambiar estado de un vehículo
    Dado que un vehículo está en estado "Activo en investigación"
    Cuando el analista confirma que fue identificado
    Entonces cambia su estado a "Identificado"
    Y el cambio queda en el historial de auditoría

  Escenario: Adjuntar imágenes a un vehículo
    Dado que estoy en la ficha de un vehículo
    Cuando subo una o más fotos
    Entonces quedan asociadas a ese vehículo y visibles en su ficha

  Escenario: Vincular un vehículo a un Informe
    Dado que estoy en la ficha de un Informe en estado "Borrador"
    Cuando busco un vehículo del catálogo por su dominio y lo vinculo
    Entonces el vehículo queda visible en la ficha del Informe
    Y el Informe queda visible en el historial de ese vehículo

  Escenario: Orden del listado de vehículos
    Dado que existen vehículos con distintos Estados y Marcas
    Cuando accedo al listado de vehículos
    Entonces por defecto los "Vigente" aparecen antes que los "Identificado"
    Y dentro de cada Estado, ordenados alfabéticamente por Marca y Modelo

  Escenario: Filtrar el listado de vehículos por Estado
    Dado que estoy en el listado de vehículos
    Cuando selecciono el filtro "Estado: Vigente"
    Entonces solo veo vehículos en estado "Vigente"

  Escenario: Cambiar el criterio de orden del listado de vehículos
    Dado que estoy en el listado de vehículos
    Cuando selecciono "Ordenar por: Alfabético"
    Entonces el listado se ordena solo por Marca y Modelo, sin agrupar por Estado
```

> **Nota de implementación (extensión 2026-08-13)**: `ListarVehiculosQuery`
> gana `Estado` (filtro opcional) y `Orden` (`Estado` | `Alfabetico`,
> default `Estado`) — dropdowns en `Vehiculos/Index.razor`, sin
> paginación al cambiar (vuelve a página 1). El orden por Estado usa una
> expresión condicional sobre el enum (Vigente antes que Identificado,
> más urgente primero, mismo criterio que el color del `StatusChip`) que
> no se traduce igual en EF Core InMemory que en Npgsql real — probado
> con Testcontainers/Postgres real (`ListarVehiculosOrdenTests`).

> **Nota de implementación (extensión 2026-08-14)**: `Vehiculo` gana
> `TipoVehiculo` (`Auto`|`Moto`|`Camioneta`|`Camion`, enum fijo,
> obligatorio) y `Cilindrada` (texto libre, obligatoria solo si
> `TipoVehiculo = Moto`, invariante validada en el constructor del
> dominio). Alta únicamente por ahora — no hay Command de edición de
> Vehículo hoy, así que Tipo/Cilindrada no se pueden corregir después de
> creado el registro. Ver `docs/03-modelo-dominio.md`, "Decisiones ya
> resueltas", para el detalle completo (incluye el default `Auto` para
> los Vehículos ya migrados del Excel histórico).

---

## HU-09 · Alta/edición manual de persona

**Como** Analista
**Quiero** dar de alta o editar una persona (sospechoso, conductor identificado, etc.)
**Para** mantener su ficha actualizada independientemente de los informes

```gherkin
Característica: Gestión de personas

  Escenario: Persona sin identificar
    Dado que una persona aparece en un informe sin DNI ni nombre
    Cuando la doy de alta con solo características físicas
    Entonces queda registrada como "no identificada" y puede completarse luego

  Escenario: Vincular persona a un vehículo
    Dado que confirmo que una persona es el conductor de un vehículo
    Cuando los vinculo desde la ficha de cualquiera de los dos (vehículo o persona)
    Entonces esa relación queda visible en ambas fichas (vehículo y persona)

  Escenario: Adjuntar imágenes a una persona
    Dado que estoy en la ficha de una persona
    Cuando subo una o más fotos
    Entonces quedan asociadas a esa persona y visibles en su ficha

  Escenario: Orden del listado de personas
    Dado que existen personas con distintos Estados (identificada o no),
    Roles y Nombres
    Cuando accedo al listado de personas
    Entonces por defecto las identificadas aparecen antes que las sin
    identificar, dentro de cada grupo ordenadas alfabéticamente por Rol,
    y dentro de cada Rol ordenadas alfabéticamente por Nombre

  Escenario: Filtrar el listado de personas por Estado y Rol
    Dado que estoy en el listado de personas
    Cuando selecciono el filtro "Estado: Identificada" y "Rol: Sospechoso"
    Entonces solo veo personas identificadas con Rol "Sospechoso"

  Escenario: Cambiar el criterio de orden del listado de personas
    Dado que estoy en el listado de personas
    Cuando selecciono "Ordenar por: Nombre"
    Entonces el listado se ordena solo alfabéticamente por Nombre, sin
    agrupar por Estado ni por Rol
```

> **Nota de implementación (extensión 2026-08-13)**: `ListarPersonasQuery`
> gana `Identificada` (filtro opcional, bool) y `Rol` (filtro opcional) más
> `Orden` (`Estado` | `Rol` | `Nombre`, default `Estado`) — dropdowns en
> `Personas/Index.razor`, sin paginación al cambiar (vuelve a página 1).
> El orden por Estado usa identificada primero (más útil para el equipo,
> mismo criterio que el `StatusChip`), luego Rol alfabético, luego Nombre
> alfabético dentro de cada Rol. `RolPersona` se mapea como `string` en
> la base (`HasConversion<string>()`, ver `PersonaConfiguration`), así
> que ordenar por esa columna ya es alfabético real, no el orden numérico
> del enum. Probado con Testcontainers/Postgres real (ver
> `ListarPersonasOrdenTests`).

> **Nota de implementación**: "vincular Persona a un Vehículo" (este escenario) es
> un vínculo **directo** entre ambas entidades (`PersonaVehiculo`), independiente
> de en qué Informe/Caso aparezcan — no confundir con "vincular un Vehículo o una
> Persona a un Informe" (vía `Evidencia`, ver invariante 7/7.a en
> `03-modelo-dominio.md`), que es una relación distinta y se documenta en HU-08
> (vínculo Informe↔Vehículo) y en el flujo de carga de Informe. Ambos vínculos
> conviven: una Persona puede estar vinculada a un Vehículo directamente y, por
> separado, ambos pueden estar vinculados (o no) al mismo Informe.

---

## HU-10 · Catálogo de cámaras

**Como** Administrador
**Quiero** mantener un catálogo de cámaras/dispositivos LPR con su ubicación
**Para** que la carga y búsqueda de evidencias sea consistente entre informes

```gherkin
Característica: Catálogo de cámaras

  Escenario: Autocompletar cámara al cargar evidencia
    Dado que el extractor reconoce el código "SL 18" en el PDF
    Cuando no existe ese código en el catálogo
    Entonces el sistema lo crea automáticamente como pendiente de completar
      ubicación, y lo notifica al Administrador
```
