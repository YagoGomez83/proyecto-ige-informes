# ADR-002 · Base de datos: PostgreSQL

## Estado
Aceptado

## Contexto
El dominio (Causa-Informe-Evidencia-Vehículo-Persona) es fuertemente
relacional. Se necesita búsqueda de texto libre sobre el relato de los
informes, sin volumen suficiente (500-5000 documentos) para justificar
un motor de búsqueda dedicado.

## Decisión
Usar **PostgreSQL** como única base de datos.

## Alternativas consideradas

| Opción | Ventajas | Desventajas |
|---|---|---|
| **PostgreSQL** (elegida) | Full-text search nativo (`tsvector` + GIN) alcanza para el volumen esperado; `JSONB` para campos flexibles (características de vehículo/persona) sin perder lo relacional; open source, sin costo de licencia — relevante para institución pública | Full-text search más limitado que Elasticsearch en ranking avanzado (no es un problema al volumen actual) |
| SQL Server | Motor robusto, buen tooling en .NET | Costo de licencia en producción on-premise (salvo Express, limitado); no aporta ventaja real sobre Postgres para este caso |
| MongoDB | Flexible para documentos con estructura variable | El dominio es relacional (FKs, integridad referencial entre Causa/Informe/Vehículo/Persona); usar Mongo obligaría a resolver esas relaciones a nivel de aplicación, sin beneficio real — *polyglot persistence sin necesidad* |

## Consecuencias
- Si el volumen de informes creciera a decenas de miles y la búsqueda de
  texto se volviera un cuello de botella, se evaluará introducir
  Elasticsearch como motor de búsqueda secundario (Postgres seguiría
  siendo la fuente de verdad).
- Los campos de "características libres" de Vehículo/Persona se modelan
  como `JSONB`, evitando una tabla de atributos dinámicos sobre-diseñada.
