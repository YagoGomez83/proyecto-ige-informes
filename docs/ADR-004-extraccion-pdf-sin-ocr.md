# ADR-004 · Extracción de datos del PDF sin OCR/ML

## Estado
Aceptado

## Contexto
Los informes históricos y futuros se generan digitalmente (Word/plantilla
→ PDF), con texto seleccionable y estructura consistente (encabezado con
Fecha de Análisis / Causa / Destino / Eleva / ID Registro, seguido de
relato y evidencias numeradas).

## Decisión
Extraer los metadatos mediante **lectura de texto plano del PDF (PdfPig)**
más **parsing por patrones/posición** (regex sobre etiquetas conocidas:
`CAUSA:`, `DESTINO:`, `ID REGISTRO:`, `IMAGEN N° X –`, etc.), sin usar OCR
ni modelos de Machine Learning.

## Alternativas descartadas

| Opción | Por qué se descarta |
|---|---|
| OCR (Tesseract) | Los PDF ya tienen texto seleccionable; aplicar OCR sería más lento, menos preciso y agregaría una dependencia pesada sin necesidad |
| ML/NLP (extracción por modelo entrenado) | Sobre-ingeniería para un formato de plantilla estable y conocido; un parser por patrones es más simple, determinístico, explicable y fácil de ajustar cuando cambie la plantilla |

## Consecuencias
- El parser debe ser tolerante a variaciones menores (ej. "FECHA DE
  ANÁLISIS" vs "Fecha de análisis", saltos de línea distintos) mediante
  normalización de texto antes de aplicar los patrones.
- Si la institución cambia radicalmente la plantilla del Word en el futuro,
  el parser deberá actualizarse — es un costo de mantenimiento aceptado,
  documentado aquí para que no sorprenda al equipo.
- Cada campo extraído lleva un nivel de confianza; si no se reconoce con
  certeza, la UI lo deja vacío y resaltado para carga manual (ver HU-01).
