---
description: Sistema de diseño visual (colores, tipografía, espaciado, formas, componentes) para las vistas Blazor de IGE Informes. Usar siempre que se cree o edite un componente en IGE.Informes.Web/Components, para que la UI mantenga una identidad profesional y consistente en vez de estilos por defecto de Bootstrap. Ver ADR-005 para la decisión de no usar Tailwind.
---

# Sistema de Diseño — IGE Informes

Dirección visual: **enterprise / analítica de datos**, orientada a densidad
de información legible (el equipo revisa muchos Casos/Informes por día) y
autoridad institucional. Nada de gradientes, nada de sombras pesadas —
jerarquía por color tonal y bordes finos, no por efectos.

## 1. Tokens de color

Definir como variables CSS globales en `wwwroot/css/design-tokens.css`:

```css
:root {
  /* Superficie */
  --color-surface: #f9f9ff;
  --color-surface-dim: #cfdaf2;
  --color-surface-container-lowest: #ffffff;
  --color-surface-container-low: #f0f3ff;
  --color-surface-container: #e7eeff;
  --color-surface-container-high: #dee8ff;
  --color-on-surface: #111c2d;
  --color-on-surface-variant: #444651;
  --color-outline: #757682;
  --color-outline-variant: #c5c5d3;

  /* Marca */
  --color-primary: #213a88;           /* Navy — navegación, acciones primarias */
  --color-on-primary: #ffffff;
  --color-primary-container: #3b52a1;
  --color-secondary: #006781;         /* Cyan/turquesa — acentos técnicos, "Exportar" */
  --color-secondary-container: #74d9fe;

  /* Semánticos — usar SOLO para estados, nunca decorativo */
  --color-safe: #0f7a3d;
  --color-safe-bg: rgba(15, 122, 61, 0.1);
  --color-warning: #92600a;
  --color-warning-bg: rgba(146, 96, 10, 0.1);
  --color-alert: #ba1a1a;
  --color-alert-bg: rgba(186, 26, 26, 0.1);

  /* Forma */
  --radius-sm: 0.25rem;   /* botones, inputs */
  --radius-lg: 0.5rem;    /* cards, modales */

  /* Espaciado (base 8px) */
  --space-xs: 0.5rem;
  --space-sm: 1rem;
  --space-md: 1.5rem;   /* gutter global */
  --space-lg: 2rem;
}
```

## 2. Tipografía

- **Inter** para toda la UI (headings, body, labels). Cargar como
  self-hosted (`wwwroot/fonts/`) — nunca vía Google Fonts CDN en un sistema
  on-premise que puede no tener salida a Internet.
- **JetBrains Mono** exclusivamente para **identificadores técnicos**: ID
  Registro (`290/2026`), dominio de vehículo (`IAK796`), DNI, Suceso. Esto
  es clave en nuestro dominio — evita ambigüedad visual entre `0` y `O`,
  `1` e `I` en datos que importan (un dominio mal leído es un error grave).

```css
.mono-data {
  font-family: 'JetBrains Mono', monospace;
  font-size: 0.8125rem;
  font-weight: 500;
  letter-spacing: 0;
}
```

| Uso | Clase/estilo |
|---|---|
| Título de página (ej. "Casos de Análisis") | `headline-md`: Inter 24px/600 |
| Título de card/sección | `headline-sm`: Inter 20px/600 |
| Texto de cuerpo (relatos, observaciones) | `body-md`: Inter 16px/400 |
| Metadatos secundarios (fecha, analista) | `body-sm`: Inter 14px/400, `--color-on-surface-variant` |
| Encabezados de tabla / labels de campo | `label-md`: Inter 12px/600, uppercase, letter-spacing 0.05em |
| ID Registro, dominio, DNI, Suceso | `.mono-data` (ver arriba) |

## 3. Mapeo de estados del dominio a color semántico

**Esta tabla es la parte más importante del skill** — nunca elegir el color
de un chip de estado "a ojo", siempre consultar esta tabla:

| Entidad.Campo | Valor | Color semántico | Justificación |
|---|---|---|---|
| `CasoAnalisis.Resultado` | Positivo | `--color-safe` | El análisis encontró algo útil |
| `CasoAnalisis.Resultado` | Negativo | `--color-outline` (neutro, no alert) | No es un problema, es un resultado válido — no usar rojo |
| `CasoAnalisis.Resultado` | Revisión | `--color-warning` | Requiere atención antes de cerrar |
| `CasoAnalisis.Estado` | Pendiente | `--color-warning` | Trabajo abierto |
| `CasoAnalisis.Estado` | Cerrado | `--color-safe` | Trabajo terminado |
| `CasoAnalisis.Estado` | Revisión | `--color-warning` | — |
| `Vehiculo.Estado` | Vigente | `--color-alert` | Vehículo activamente buscado — máxima atención visual |
| `Vehiculo.Estado` | Identificado | `--color-safe` | Caso resuelto para ese vehículo |
| `Informe.Estado` | Borrador | `--color-warning` | Incompleto, no publicado |
| `Informe.Estado` | Publicado | `--color-safe` | Definitivo |

> Nunca usar `--color-alert` (rojo) para algo que no requiera acción
> humana inmediata — perdería su fuerza de señal si se usa decorativamente
> (ver "Density" en la sección de layout).

## 4. Componente: Status Chip

```css
.status-chip {
  display: inline-block;
  padding: 0.125rem 0.5rem;
  border-radius: var(--radius-sm);
  font-family: Inter, sans-serif;
  font-size: 0.75rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.03em;
}
.status-chip--safe    { background: var(--color-safe-bg);    color: var(--color-safe); }
.status-chip--warning { background: var(--color-warning-bg); color: var(--color-warning); }
.status-chip--alert   { background: var(--color-alert-bg);   color: var(--color-alert); }
```

Componente Blazor sugerido: `Shared/StatusChip.razor` que reciba
`Estado` (string) y resuelva internamente la clase CSS según la tabla de
la sección 3 — **no** duplicar ese mapeo en cada página que muestre un chip.

## 5. Componente: Card

Contenedor principal para listados de Casos/Informes y para el detalle.

```css
.card {
  background: var(--color-surface-container-lowest);
  border: 1px solid var(--color-outline-variant);
  border-radius: var(--radius-lg);
  padding: 1rem; /* densidad: interior de card más ajustado que el margen global */
}
/* Borde superior de 4px cuando el item requiere atención inmediata */
.card--alert {
  border-top: 4px solid var(--color-alert);
}
```

Sombras: **no usar** salvo en hover de elementos clickeables (`.card:hover`
con sombra difusa, baja opacidad, tinte navy) — nunca sombra estática.

## 6. Layout general

- **Sidebar fijo a la izquierda en desktop** (navy, `--color-primary` de
  fondo) con las secciones: Overview/Dashboard, Casos, Informes, Vehículos,
  Personas, Alertas (vehículos vigentes con acción "Detener"), Búsqueda,
  Configuración — coincide con los roles y entidades del modelo.
- **En mobile**: sidebar colapsa a barra inferior con las 2-3 acciones más
  usadas (según el research de UX real, probablemente "Casos", "Buscar",
  "Nuevo Caso").
- Grilla de 12 columnas desktop → 4 columnas mobile.
- Margen global de página: `--space-md` (24px). Padding interno de cards:
  `--space-sm` (16px) — más ajustado, para priorizar densidad de datos.
- Layout de dos paneles para el detalle de Informe (lista a la izquierda +
  visor de PDF/detalle a la derecha) es un patrón válido para la vista de
  "Informes" — coincide con cómo ya trabaja el equipo hoy revisando PDFs.

## 7. Botones e inputs

- **Primario**: fondo sólido `--color-primary`, texto blanco, sin gradiente,
  `border-radius: var(--radius-sm)`. Usar para acciones principales
  ("Guardar Caso", "Publicar Informe").
- **Secundario**: outline con `--color-secondary` (cyan), para acciones
  técnicas no destructivas ("Exportar", "Generar Informe", "Buscar").
- **Destructivo** (eliminar): outline o texto en `--color-alert`, nunca
  como botón primario sólido — evita clics accidentales en acciones
  irreversibles.
- **Inputs**: borde 1px `--color-outline-variant`, focus con anillo de 2px
  en `--color-secondary` (cyan) — nunca solo un cambio de borde, el foco
  debe ser visualmente inequívoco (accesibilidad).

## 8. Qué NO hacer

- No usar rojo/alert para elementos decorativos o branding — se reserva
  exclusivamente para estados que requieren atención (ver sección 3).
- No usar sombras pesadas ni gradientes — rompe la identidad "institucional,
  estructurada" que busca este sistema.
- No cargar fuentes desde un CDN externo (Google Fonts, etc.) — este es un
  sistema on-premise, puede no tener salida a Internet. Self-host las
  fuentes en `wwwroot/fonts/`.
- No dejar que cada página reinvente su propio chip de estado o su propia
  card — reutilizar los componentes compartidos de `Shared/`.
