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
| **Dependencia** | Organismo externo que solicita el análisis o el Informe: Comisaría, Fiscalía, Juzgado, División de investigación, o Unidad Regional (UR). Algunas Dependencias (típicamente Comisarías) tienen **jurisdicción geográfica**: una colección de **Barrio** bajo su cobertura. Otras (Fiscalía, Juzgado) no la tienen — el campo queda vacío. Una Dependencia de tipo Comisaría puede pertenecer a una **Unidad Regional** (otra Dependencia con `Tipo = UnidadRegional`) mediante `UnidadRegionalId` — una UR agrupa varias Comisarías/Jurisdicciones bajo su mando. |
| **Barrio** | Zona geográfica catalogada (barrio, zona rural, tramo de ruta) que puede estar bajo la jurisdicción de una o más Dependencias. Catálogo simple administrado por el Administrador — sin geometría/mapa, solo nombre. Puede asociarse opcionalmente a la **Localidad** donde está (`Barrio.LocalidadId`, nullable) — no como jerarquía obligatoria, sino para distinguir dos Barrios homónimos en ciudades distintas (ej. "Barrio Norte" en San Luis vs. "Barrio Norte" en Villa Mercedes). |
| **Localidad** | Ciudad, pueblo o paraje de la provincia donde está físicamente instalada una Cámara (ej. `Arizona`, `Cerro de Oro`, `Estancia Grande`, `Potrero de Los Funes`). Catálogo simple administrado por el Administrador — sin geometría/mapa, solo nombre. **Distinto de Barrio**: Barrio es la jurisdicción geográfica de una Dependencia (usado para Casos/Informes); Localidad es un atributo geográfico de la Cámara, viene del relevamiento físico del catálogo de cámaras y no tiene relación con Dependencia. |
| **Centro de Control de Cámaras (CCC)** | Catálogo de los centros que monitorean cámaras, uno por ciudad cabecera: CCCSL (San Luis), CCCVM (Villa Mercedes), CCCME (Merlo), CCCJD (Justo Daract). Toda Cámara pertenece a un CCC. |
| **Evidencia** | Cada captura individual documentada dentro de un Informe (una "Imagen N°X"): cámara/dispositivo de origen, fecha y hora exacta, descripción y archivo de imagen. |
| **Cámara / Dispositivo** | Fuente de una Evidencia o mencionada en un Caso. Código identificador (`SL 18`, `JK 51`, `VM 86`, `LP 217`) más ubicación. Dos tipos: Domo de monitoreo o LPR (Lector de Patentes). Puede pertenecer opcionalmente a una **Dependencia** (una Domo dentro de la jurisdicción de una Comisaría) — una LPR en ruta o en un paso limítrofe puede no tener Dependencia asociada. Tiene una **Localidad** (dónde está instalada) y un **Centro de Control de Cámaras** que la monitorea. **`Codigo` no es único**: el relevamiento real muestra el mismo código para varias cámaras de una misma instalación agrupada (ej. un peaje o una planta verificadora con múltiples cámaras) — se diferencian por `Ubicacion`, no por `Codigo`. |
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
- **Jurisdicción geográfica no es universal**: no todo `Tipo` de
  `Dependencia` tiene Barrios asociados (una Comisaría sí, una Fiscalía o
  Juzgado normalmente no) — no forzar la relación como obligatoria ni
  restringirla por `Tipo` en el dominio; el campo simplemente queda vacío
  cuando no aplica.
- **`Camara.DependenciaId` es opcional**: una Domo dentro de la
  jurisdicción de una Comisaría se vincula a esa Dependencia; una LPR en
  ruta o en un paso limítrofe puede no pertenecer a ninguna.
- **`Camara.Codigo` no es único**: a diferencia de `Dependencia.Nombre`, el
  relevamiento real (`docs/camaras.xlsx`) trae códigos repetidos entre
  varias cámaras de una misma instalación agrupada (ej. un peaje con 22
  cámaras bajo el mismo código). No forzar unicidad en el dominio ni en la
  base — diferenciar por `Ubicacion`.
- **`Localidad` no es `Barrio`**: no unificar ambos catálogos aunque
  compartan la forma (solo nombre) y ahora tengan una relación entre sí.
  `Barrio` cuelga de `Dependencia` (jurisdicción para Casos/Informes);
  `Localidad` cuelga de `Camara` (dónde está instalada físicamente). Son
  dos vocabularios geográficos independientes que pueden solaparse en
  nombre sin ser la misma entidad — relacionarlos (`Barrio.LocalidadId`)
  no los fusiona, solo evita que dos Barrios homónimos en ciudades
  distintas choquen entre sí.
- **`Barrio.Nombre` es único solo dentro de la misma `Localidad`**
  (actualizado 2026-07-29, ver HU-13 en `epic-04-gestion-catalogos.md`):
  antes era único a nivel global; se detectó que dos ciudades distintas
  pueden tener un barrio con el mismo nombre. La unicidad real es la
  combinación `(Nombre, LocalidadId)`. Un Barrio sin Localidad asignada
  (`LocalidadId = null`) **no** garantiza unicidad de `Nombre` frente a
  otro Barrio también sin Localidad — es un estado transitorio de carga
  incompleta, mismo criterio que `Camara` sin `Dependencia`/`Localidad`.
- **Jerarquía de Unidad Regional**: se modela como auto-referencia dentro
  de `Dependencia` (`UnidadRegionalId`, nullable, FK a otra `Dependencia`
  con `Tipo = UnidadRegional`) en vez de una entidad separada — reutiliza
  el catálogo existente en lugar de duplicar el concepto de "organismo
  externo".
