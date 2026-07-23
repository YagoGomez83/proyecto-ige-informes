# ADR-006 · Tablero de analítica: Chart.js vía JS interop + exportación CSV

## Estado
Aceptado

## Contexto
HU-06 (Épica 02, Fase 4) requiere un tablero con gráficos de conteo de
`CasoAnalisis` por Dependencia, Tipo de Incidente, Analista y Resultado,
más exportación de esos datos. No había ninguna librería de gráficos ni de
generación de planillas instalada en el repo, y ambas son dependencias
nuevas que este proyecto exige proponer explícitamente antes de agregar
(ver CLAUDE.md, "Qué NO hacer").

## Decisión

**Gráficos**: Chart.js cargado como asset estático en `wwwroot/lib/chartjs`
(vendored, sin CDN por el criterio on-premise de ADR-003), invocado vía
JS interop (`IJSRuntime`) desde componentes Blazor Server. No se agrega
ningún paquete NuGet para esto.

**Exportación**: CSV generado en el servidor con `System.Text.StringBuilder`
(sin dependencia nueva), descargado vía `IJSRuntime` (`downloadFile` helper
JS) o endpoint de archivo. No se usa ClosedXML/EPPlus por ahora.

## Alternativas consideradas

| Opción | Ventajas | Desventajas |
|---|---|---|
| **Chart.js + JS interop** (elegida) | Estándar de facto, liviana, sin paquete NuGet server-side; se vendorea un solo archivo JS | Requiere JS interop manual (dos mundos: C#/JS) |
| SVG hecho a mano | Cero dependencias, integración directa con `ige-design-system` | Mucho más código propio para ejes/leyendas/tooltips — no se justifica para el alcance de HU-06 |
| Componente Blazor nativo (Radzen.Blazor u otro) | Sin JS interop, Razor puro | Dependencia NuGet grande de terceros que condicionaría el resto de la UI; mismo criterio de sobre-ingeniería que ADR-003 |
| **CSV con StringBuilder** (elegida) | Sin dependencia nueva, se abre bien en Excel, fácil de testear | Sin estilos/formato de planilla real |
| Excel real (ClosedXML/EPPlus) | Mejor experiencia visual para el Supervisor | Dependencia NuGet nueva en Infrastructure; se puede revisitar si el Supervisor lo pide explícitamente |

## Consecuencias
- Se agrega `wwwroot/lib/chartjs/chart.umd.min.js` (vendored) y un módulo
  JS propio `wwwroot/js/tablero-analitica.js` con las funciones de
  render/destroy de gráficos, invocadas desde el código-behind del
  componente Blazor.
- La exportación CSV vive como un método de servicio en `Application`
  (o `Infrastructure` si necesita acceso a EF Core), reutilizable por los
  cuatro reportes de HU-06.
- Si en el futuro se pide un Excel con formato real, esta decisión se
  revisita — no bloquea agregar ClosedXML más adelante.
