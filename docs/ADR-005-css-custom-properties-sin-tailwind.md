# ADR-005 · Estilos visuales: CSS custom properties + CSS isolation (sin Tailwind)

## Estado
Aceptado

## Contexto
Se definió un sistema de diseño (paleta, tipografía, espaciado, formas)
para que la UI de Blazor Server tenga una identidad visual profesional y
consistente, en vez de depender de los estilos por defecto de Bootstrap
(plantilla estándar de Blazor).

## Decisión
Implementar el sistema de diseño con **CSS custom properties** (variables
CSS nativas) definidas globalmente en `wwwroot/css/design-tokens.css`, y
usar la **CSS isolation** propia de Blazor (`Componente.razor.css`) para los
estilos específicos de cada componente. No se incorpora Tailwind CSS.

## Alternativas consideradas

| Opción | Ventajas | Desventajas |
|---|---|---|
| **CSS custom properties + CSS isolation** (elegida) | Cero dependencias de Node/npm; los tokens de diseño quedan en un solo lugar y se referencian con `var(--nombre)`; la CSS isolation de Blazor ya resuelve el scoping por componente sin herramientas extra | Menos utilidades "atajo" que Tailwind (hay que escribir alguna clase utilitaria propia si se necesita) |
| Tailwind CSS | Ecosistema grande, utilidades listas | Requiere pipeline de build con Node/npm — una dependencia de infraestructura más para mantener en un proyecto on-premise .NET, sin necesidad real dado el tamaño del equipo (mismo criterio que ADR-003 con Kubernetes) |
| Bootstrap (plantilla por defecto de Blazor) | Ya viene con el template | No transmite la identidad visual "enterprise/analítica de datos" que se busca; requeriría sobre-escribir casi todas sus clases igual |

## Consecuencias
- Los tokens de diseño (colores, tipografía, espaciado, radios) viven en
  `wwwroot/css/design-tokens.css` como variables `:root`, documentados en
  detalle en el skill `.claude/skills/ige-design-system/SKILL.md`.
- Cualquier componente Blazor nuevo debe consumir esas variables (`var(--color-primary)`,
  etc.) en su archivo `.razor.css`, nunca hardcodear valores de color/tipografía.
- Si en el futuro el equipo crece y se justifica una librería de utilidades,
  esta decisión se revisita — no bloquea una migración posterior.
