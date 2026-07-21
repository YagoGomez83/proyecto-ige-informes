# 00 · Visión y Alcance

## Contexto

El **Instituto de Gestión de Emergencias 4.0 (IGE 4.0)** de San Luis cuenta con un
Equipo de Analítica que recibe pedidos de dependencias externas (comisarías,
fiscalías, juzgados, divisiones de investigación) y del propio centro de
monitoreo para analizar cámaras de videovigilancia y lectores de patentes
(LPR). Cada pedido se registra hoy como una fila en una planilla Excel mensual
(años 2022-2026, ~50 hojas) y, quizás, deriva en un **Informe Especial en PDF**
formal cuando la dependencia solicitante lo requiere para su expediente.

## Problema a resolver

1. El registro diario de casos vive en una planilla Excel de más de 4 años de
   historia, con estructura de columnas inconsistente entre hojas, sin poder
   buscar por vehículo, persona, dependencia o código de incidente de forma
   confiable.
2. Los Informes Especiales en PDF (subconjunto formal de esos casos) se
   almacenan sueltos en Google Drive, sin vínculo estructurado a la fila del
   caso que les dio origen.
3. No hay forma sistemática de generar analítica de gestión (casos por
   dependencia, por código de incidente, por analista, por resultado
   positivo/negativo, evolución en el tiempo).
4. El catálogo de vehículos en alerta (robados, con inhibidores, vinculados a
   narcotráfico, robo de cubiertas, pedidos especiales) está repartido en
   múltiples hojas de otro Excel, con datos duplicados y desactualizados.

## Objetivo del sistema

Construir un sistema de **gestión de casos de análisis, documentación e
indexación** que:

- **Reemplace la planilla de seguimiento diario**: cada pedido de análisis
  (llegue por AMPSUM, suceso, WhatsApp del comisario, o pedido interno) se
  carga como un **Caso de Análisis**, con su código de incidente, dependencia,
  analistas asignados, vehículo/persona involucrados, cámaras analizadas,
  relato y resultado (Positivo/Negativo/Revisión).
- Permita que un Caso de Análisis **escale opcionalmente** a un **Informe
  Especial** (PDF formal) cuando la instrucción lo requiere — el Informe
  siempre nace de un Caso, nunca al revés.
- Ingiera los Informes PDF (históricos y nuevos) extrayendo automáticamente
  sus metadatos estructurados.
- Mantenga un catálogo único de **Vehículos** en alerta, con categoría/motivo
  (Robado, Narcotráfico, Inhibidores, Robo de Cubiertas, Pedido Especial),
  estado (Vigente/Identificado), acción a realizar al detectarlo (Detener/
  Identificar) y a quién avisar.
- Mantenga un catálogo de **Personas** vinculadas a casos/informes con su rol
  (Denunciante, Damnificado, Sospechoso, Conductor, Testigo).
- Ofrezca tableros de analítica (casos por dependencia, por código de
  incidente, por analista, por resultado, evolución temporal) que reemplacen
  el conteo manual que hoy se hace sobre el Excel.

## Fuera de alcance (v1)

- El sistema **no genera** el PDF del Informe (el analista sigue redactando
  en Word/plantilla externa). El sistema solo indexa el PDF final.
- **No se migra el histórico de Casos de Análisis** (los ~4 años en Excel).
  El equipo sigue llevando ese histórico en la planilla actual en paralelo;
  el sistema nuevo arranca registrando Casos desde cero a partir de su
  puesta en producción. Solo se migran: el catálogo de Vehículos y los PDFs
  de Informes históricos en Drive.
- No incluye reconocimiento facial ni matching automático de imágenes por
  similitud visual (catálogo + búsqueda manual por atributos alcanza).
- No reemplaza TRAMIX (sistema de expediente digital) ni gestiona el circuito
  administrativo formal de la causa — solo referencia sus datos (carátula,
  pieza sumarial).

## Actores

| Actor | Descripción |
|---|---|
| Analista / Operador | Carga y actualiza Casos de Análisis, carga Informes, consulta/busca, gestiona catálogo de vehículos/personas |
| Supervisor de Equipo Analítica | Todo lo del Analista + tableros de analítica + gestión de usuarios |
| Administrador | Configuración del sistema, catálogos (tipos de incidente, cámaras), auditoría, usuarios |

## Restricciones técnicas conocidas

- Despliegue **on-premise** (servidor propio de la institución).
- 10-30 usuarios concurrentes esperados.
- Sin Active Directory/LDAP disponible: gestión de usuarios propia.
- Volumen histórico a migrar: 500-5000 Informes PDF + catálogo de vehículos
  desde el Excel de relevamiento (sin migrar el histórico de Casos de
  Análisis, ver "Fuera de alcance").

## Criterios de éxito

- Un analista carga un Caso de Análisis nuevo en menos tiempo del que le toma
  hoy escribir la fila en Excel.
- Un analista encuentra un caso o informe por dominio de vehículo, DNI de
  persona o número de suceso/AMPSUM en menos de 10 segundos.
- El supervisor genera un reporte de "casos por dependencia y código de
  incidente" del último trimestre sin abrir Excel.
