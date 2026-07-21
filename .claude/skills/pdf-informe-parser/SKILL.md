---
description: Conocimiento específico de la plantilla de "Informe Especial - Análisis Cámaras de Videovigilancia" del IGE 4.0, para implementar o ajustar el parser de extracción de PDF (PdfPig). Usar cuando se trabaje en IGE.Informes.Infrastructure/PdfParsing o se ajuste el mapeo de campos del Informe.
---

# Plantilla de Informe Especial — reglas de extracción

Basado en el análisis de 3 informes reales (IDs 08/2026, 290/2026, 293/2026).
Estas son las reglas de extracción por patrón — ver ADR-004 en
`docs/04-arquitectura/adr/ADR-004-extraccion-pdf-sin-ocr.md` para el porqué
de este enfoque (sin OCR/ML).

## Encabezado (página 1, siempre presente)

Los campos vienen en líneas con etiqueta en mayúsculas seguida de `:`.
Normalizar espacios y saltos de línea antes de aplicar regex.

| Campo | Patrón de línea | Ejemplo real | Notas |
|---|---|---|---|
| Fecha de análisis | `FECHA DE ANÁLISIS:` | `14 DE JULIO DE 2026` | Formato texto en español, no numérico — parsear con diccionario de meses |
| Causa (carátula) | `CAUSA:` | `"AV. INFRACCION LEY 23.737"` | Viene entre comillas tipográficas `“ ”` — normalizar a comillas simples antes de extraer |
| Destino | `DESTINO:` | `DIVISION LUCHA CONTRA EL NARCO TRAFICO DG-8` | Puede partirse en 2 líneas dentro del PDF (salto de línea en medio del texto) — unir líneas hasta el próximo campo en mayúsculas |
| Eleva | `ELEVA:` | Siempre `INSTITUTO DE GESTIÓN DE EMERGENCIAS 4.0` | Constante — no necesita extracción real, se puede fijar por configuración |
| ID Registro | `ID REGISTRO:` | `08/2026`, `290/2026` | Formato `NNN/AAAA` — este es el campo más crítico, es la clave única del Informe. Si no matchea el patrón, **no** publicar automáticamente, marcar para revisión manual |

## Relato / nota (párrafo narrativo, después del encabezado)

Empieza después de "ID REGISTRO:" y termina antes del primer bloque
recuadrado en verde (marcador de inicio del análisis, texto tipo
"En el presente informe se procede a realizar...").

Dentro del relato buscar (todas opcionales, con expresiones regulares
tolerantes a mayúsculas/minúsculas y orden variable):
- Menciones a **vehículos**: patrón `marca\s+(\w+)[,.]?\s*modelo\s+([\w\s]+)`
  seguido de `dominio\s*(visible)?\s*:?\s*([A-Z]{2,3}[\s-]?\d{2,3})`.
  El dominio puede venir con espacio (`IAK 796`) o sin espacio (`IAK796`) —
  normalizar quitando espacios internos antes de guardar, pero conservar
  el original en un campo de auditoría.
- Menciones a **personas** con rol: buscar `DNI\s*N?°?:?\s*([\d.]+)` cerca
  de palabras como "damnificado", "denunciante" para inferir el rol.
- **Pieza sumarial**: patrón `pieza sumarial N°\s*([\d/]+)` — este valor va
  al campo `Causa.nro_pieza_sumarial`, **no** al Caso de Análisis (ver
  `03-modelo-dominio.md` — la Causa pertenece al Informe).

## Bloque de Evidencias (una por imagen)

Patrón repetido: `IMAGEN N°\s*(\d+)\s*[–-]\s*(.+)` seguido del párrafo
descriptivo. La cámara/dispositivo de origen aparece en el **título de la
captura de pantalla** (no en el texto corrido), con formato:
`<CÓDIGO> - <ubicación> - <fecha> <hora>` (ej. `SL 18 - Lafinur y Junin -
2/7/2026 20:40:42.904`) o, para lectores de patente, el formato de
"Capture Details" con `Location:`, `Capture Time:`, `Plate Number:`.

- Si el título de imagen no matchea ninguno de los dos formatos conocidos,
  guardar la Evidencia igual pero dejar `camara_id` sin resolver (nullable)
  y marcar para completar manualmente — **no bloquear** la carga por esto.
- Las imágenes N° pueden venir dobles en una misma página (ej. "IMAGEN N°
  15 Y N° 16") — tratar como dos registros de Evidencia separados aunque
  compartan párrafo descriptivo.

## Pie de página (constante, no requiere extracción variable)

Siempre incluye: cláusula de retención de grabaciones (30 días — dato
informativo, no se persiste como campo), tabla de "Analistas
intervinientes" / "Analistas Firmante" / "Supervisor Equipo Analítica"
(mapear por nombre a `Usuario` existente; si el nombre no matchea ningún
usuario existente, crear un registro de auditoría de advertencia, no crear
usuario automáticamente) y el coordinador con celular (constante, no
persistir como dato variable del Informe).

## Casos límite ya observados (para tests)

- Informe sin vehículo, solo persona (ver informe 290/2026 — caso de hurto
  con denunciante identificado y vehículo del sospechoso).
- Informe con dos vehículos distintos en un mismo documento con secciones
  separadas de seguimiento (ver informe 08/2026 — Toyota Hilux + Chevrolet
  Celta).
- Dominio no confirmado, marcado explícitamente como "no visible" en el
  texto (ver informe 293/2026 — moto sin patente visible).
