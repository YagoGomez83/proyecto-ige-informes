# 01 · Glosario de Dominio (Ubiquitous Language)

> Este glosario es el lenguaje único entre negocio, código y documentación.
> Los nombres de clases/entidades en el código deben usar estos mismos términos.

| Término | Definición |
|---|---|
| **Caso de Análisis** | Unidad central de trabajo: cada pedido de revisión de cámaras que recibe el equipo, típicamente originado por un **llamado al 911** (o un aviso interno/pedido directo de un comisario). Reemplaza la fila de la planilla Excel diaria. Tiene código de incidente, dependencia/jurisdicción del llamado, analistas asignados, vehículo/persona involucrados, cámaras analizadas, relato/observaciones y un resultado. Puede o no dar origen a uno o más Informes. |
| **Informe (Informe Especial)** | Documento PDF formal que una **Dependencia solicita sobre un Caso de Análisis ya trabajado**, para su propio expediente. Siempre nace de un Caso de Análisis (`Informe.caso_analisis_id` obligatorio); un mismo Caso puede originar más de un Informe si distintas dependencias lo solicitan. Identificado por su **ID Registro** (`NNN/AAAA`). |
| **ID Registro** | Identificador correlativo/año del Informe dentro del IGE 4.0 (ej. `290/2026`). |
| **Suceso** | Número de llamado al **911** que originó el Caso de Análisis. Referencia interna del sistema de emergencias, **no** un dato judicial. Nullable: no todo caso llega por 911. |
| **Causa** | Expediente judicial/policial de la **Dependencia solicitante** (carátula + N° de pieza sumarial + circunscripción judicial). Pertenece al Informe, no al Caso — se completa cuando una dependencia formaliza su pedido de documentación. |
| **Pieza sumarial** | Número de expediente que la Dependencia le asigna a su Causa (ej. `7070029/26`). Parte de la Causa; distinto del Suceso (911) del Caso. |
| **Código de Incidente** | Catálogo de tipificación policial usado para clasificar el Caso de Análisis (ej. `164 - ROBO`, `162 - HURTO`, `02 - ASALTO A MANO ARMADA`, `25 - PERSONA SOSPECHOSA`). Es independiente de si el caso termina generando un Informe: ese es un evento posterior, disparado por el pedido de una Dependencia. |
| **Estado del Caso** | Pendiente, Cerrado o En Revisión — refleja si el trabajo de análisis del caso está terminado. |
| **Resultado del Caso** | Positivo, Negativo o Revisión — indica si el análisis de cámaras arrojó un hallazgo útil para la investigación. Dimensión clave de analítica de gestión. |
| **Dependencia** | Organismo externo que solicita el análisis o el Informe: Comisaría, Fiscalía, Juzgado, División de investigación, o Unidad Regional (UR). |
| **Evidencia** | Cada captura individual documentada dentro de un Informe (una "Imagen N°X"): cámara/dispositivo de origen, fecha y hora exacta, descripción y archivo de imagen. |
| **Cámara / Dispositivo** | Fuente de una Evidencia o mencionada en un Caso. Código identificador (`SL 18`, `JK 51`, `VM 86`, `LP 217`) más ubicación. Dos tipos: Domo de monitoreo o LPR (Lector de Patentes). |
| **Vehículo** | Rodado en catálogo de vigilancia. Atributos: marca/modelo, color, dominio (puede ser parcial/incierto), **categoría de alerta** (Robado, Narcotráfico, Inhibidores, Robo de Cubiertas, Pedido Especial — puede tener más de una), **estado** (Vigente | Identificado), **acción a realizar** al detectarlo (Detener | Identificar), **avisar a** (dependencia/persona de contacto), **fecha de baja** (cuándo dejó de estar en vigilancia activa). |
| **Persona** | Individuo mencionado o identificado en un Caso/Informe, con un **rol** (Denunciante, Damnificado, Sospechoso, Conductor identificado, Testigo). Puede tener DNI si está identificada, o solo características físicas si no. |
| **Analista / Operador** | Usuario del Equipo de Analítica. Un Caso puede tener varios analistas asignados (columna "Operadores" / "Creador ID" en la planilla histórica — se modela como relación N:M con rol). |
| **Analista firmante** | Analista que firma y da por válido un Informe. |
| **Supervisor de Equipo Analítica** | Usuario que revisa/aprueba el trabajo del equipo y consume tableros de analítica de gestión. |
| **Mapeo geográfico / Recorrido** | Imagen de mapa adjunta a un Informe que ilustra el trayecto reconstruido. |
| **TRAMIX** | Sistema de Expediente Digital de la Agencia de Ciencia y Tecnología San Luis. Fuera de alcance del sistema (solo se referencia). |

## Notas de modelado importantes

- **Caso de Análisis vs Informe**: no son lo mismo. El Caso es la unidad de
  trabajo diaria (siempre existe); el Informe es la salida documental formal
  (existe solo quizás). Modelar el Informe como dependiente del Caso, nunca
  al revés.
- **Suceso ausente**: no todo Caso llega por un llamado al 911 (columna con
  "-" o vacía en el histórico) — debe ser un campo opcional.
- **Causa ausente**: un Caso puede nunca tener una Dependencia solicitando
  Informe, y por lo tanto nunca tiene Causa asociada. Eso es normal, no un
  error de carga.
- **Vehículo con múltiples categorías de alerta**: un mismo vehículo podría,
  en teoría, estar señalado por más de un motivo (ej. robado Y vinculado a
  narcotráfico) — modelar como colección de etiquetas, no como una columna
  única.
- **Dominio incierto**: nunca validar formato estricto de patente — hay
  errores de tipeo reales en los datos existentes (ej. espacios, dígitos
  cambiados entre fuentes del mismo caso).
- **Características libres**: no modelar como columnas fijas — usar un
  campo de observaciones/tags flexible.
