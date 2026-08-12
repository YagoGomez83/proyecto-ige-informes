# Épica 01 · Gestión e Ingesta de Informes

## HU-01 · Cargar un informe nuevo

**Como** Analista
**Quiero** subir el PDF de un informe recién elaborado
**Para** que quede indexado y disponible para búsqueda sin recargar los datos a mano

### Criterios de aceptación

```gherkin
Característica: Carga de informe

  Escenario: Carga exitosa con extracción automática
    Dado que soy un Analista autenticado
    Cuando subo un archivo PDF de un Informe Especial
    Entonces el sistema extrae automáticamente: ID Registro, fecha de análisis,
      causa, dependencia destino, relato y menciones de vehículos/personas
    Y me muestra una pantalla de confirmación con los datos extraídos
    Para que yo los revise y corrija antes de guardar definitivamente

  Escenario: ID Registro duplicado
    Dado que subo un PDF cuyo ID Registro ya existe en el sistema
    Entonces el sistema me advierte del duplicado antes de guardar
    Y me permite decidir si reemplazar la versión existente o cancelar

  Escenario: Extracción incompleta o de baja confianza
    Dado que el PDF no sigue exactamente la plantilla esperada
    Cuando el extractor no puede reconocer uno o más campos
    Entonces el sistema me muestra el/los campo(s) vacíos resaltados
    Y no me deja publicar el informe hasta completarlos manualmente

  Escenario: Confirmar la carga sin Circunscripción judicial
    Dado que estoy confirmando la carga de un PDF con Carátula y N° de
      Pieza Sumarial completos
    Y la Circunscripción judicial no está especificada
    Cuando confirmo la carga
    Entonces el informe se guarda con su Causa (Carátula y Pieza
      Sumarial) sin Circunscripción judicial
```

### Notas técnicas
- El worker de extracción corre de forma asíncrona (background job); la UI
  muestra estado "Procesando" y notifica cuando está listo para revisión.
- Reutiliza el mismo parser que la migración histórica (ver HU-04).

### Listado de Informes: orden, Causa/Dependencia visibles y filtro "sin Causa" (extensión de HU-01)

> Contexto (2026-08-12): tras la migración masiva, la mayoría de los
> Informes quedaron sin `Causa` vinculada (el auto-match por N° de Pieza
> Sumarial requiere que ya exista una `Causa` con ese número exacto — ver
> HU-02/HU-04). El usuario necesita revisar y completar esas Causas a
> mano, informe por informe, y el listado `/informes` no ayudaba: no
> mostraba Causa ni Dependencia, y el orden por Fecha de Análisis era fijo
> (sin poder invertirlo).

```gherkin
Característica: Listado de Informes con orden y filtro

  Escenario: Orden por Fecha de Análisis descendente (por defecto)
    Dado que existen Informes con distintas fechas de análisis
    Cuando accedo al listado de Informes sin especificar orden
    Entonces los veo ordenados de la fecha más reciente a la más antigua

  Escenario: Invertir el orden a ascendente
    Dado que estoy viendo el listado de Informes
    Cuando hago clic en el encabezado "Fecha de análisis" para invertir el orden
    Entonces los veo ordenados de la fecha más antigua a la más reciente

  Escenario: Ver Causa y Dependencia en el listado
    Dado que un Informe tiene una Causa vinculada y una Dependencia destino
    Cuando veo el listado de Informes
    Entonces la fila de ese Informe muestra la Carátula de su Causa y el
    nombre de su Dependencia destino

  Escenario: Informe sin Causa vinculada todavía
    Dado que un Informe migrado no tiene Causa vinculada
    Cuando veo el listado de Informes
    Entonces la fila de ese Informe indica visualmente que falta la Causa
    (ej. "Sin Causa"), en vez de dejar la columna vacía sin explicación

  Escenario: Filtrar solo los Informes sin Causa
    Dado que existen Informes con y sin Causa vinculada
    Cuando activo el filtro "Mostrar solo sin Causa"
    Entonces el listado muestra únicamente los Informes sin Causa vinculada

  Escenario: Desactivar el filtro
    Dado que el filtro "Mostrar solo sin Causa" está activo
    Cuando lo desactivo
    Entonces vuelvo a ver todos los Informes, con o sin Causa
```

### Notas de modelado

- No se agrega ninguna entidad nueva. Se agrega `InformeListadoDto`
  (`Application/Informes/Queries/ListarInformesPaginado/`), específico de
  este listado — **no reutiliza** `InformeResumenDto`
  (`Queries/ListarInformesPorCaso/`), que sigue usándose sin cambios en
  `Casos/Detalle.razor` y no necesita Causa/Dependencia por nombre.
- `ListarInformesPaginadoQuery` gana dos parámetros opcionales:
  `OrdenDireccion` (`Asc`/`Desc`, default `Desc` — mismo comportamiento
  que hoy) y `SoloSinCausa` (`bool`, default `false`).
- El Handler resuelve `CausaCaratula` y `DependenciaNombre` con un join a
  `Causa`/`Dependencia` — `CausaCaratula` es nullable (Informe sin Causa
  vinculada); `DependenciaNombre` no lo es (`DependenciaDestinoId` es
  obligatorio en `Informe`).
- Mismo criterio de autorización y auditoría que ya tenía el Handler
  (`[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]`,
  `RegistrarAccesoAsync("Listado", ...)`) — los 3 roles ya ven el
  universo completo de Informes, el filtro no introduce ningún control de
  acceso nuevo, es solo un `WHERE` sobre datos que el usuario ya podía ver.

---

## HU-02 · Editar / corregir metadatos de un informe

**Como** Analista
**Quiero** corregir los datos extraídos automáticamente de un informe
**Para** garantizar que la ficha quede correcta aunque la extracción falle parcialmente

```gherkin
Característica: Edición de metadatos

  Escenario: Corrección de campos
    Dado que tengo abierto un informe en estado Borrador
    Cuando modifico causa, dependencia, vehículos o personas asociadas
    Entonces los cambios se guardan y quedan registrados en el log de auditoría
      con mi usuario y fecha/hora

  Escenario: Ver el PDF original mientras edito un informe
    Dado que tengo abierto un informe en estado Borrador para editar
    Cuando cargo la pantalla de edición
    Entonces se muestra el PDF del informe junto al formulario
    Para que pueda verificar cada dato contra el documento original antes
      de corregirlo

  Escenario: Crear una Dependencia nueva sin salir de la edición
    Dado que soy Administrador y tengo abierto un informe en estado
      Borrador para editar
    Y la Dependencia destino real del informe no existe en el catálogo
    Cuando creo la Dependencia nueva desde "+ Nueva Dependencia" en la
      pantalla de edición
    Entonces la Dependencia queda disponible en el selector
    Y queda seleccionada automáticamente, sin perder el resto de los
      cambios que ya había hecho en el informe

  Escenario: Corregir el ID Registro de un informe en Borrador
    Dado que tengo abierto un informe en estado Borrador con ID Registro "100/2022"
    Cuando corrijo el ID Registro a "101/2022"
    Y ningún otro informe tiene ya el ID Registro "101/2022"
    Entonces el ID Registro del informe pasa a ser "101/2022"
    Y el cambio queda registrado en el log de auditoría

  Escenario: Rechazar un ID Registro que ya existe en otro informe
    Dado que ya existe un informe Publicado o Borrador con ID Registro "101/2022"
    Y tengo abierto otro informe distinto en estado Borrador
    Cuando intento corregir su ID Registro a "101/2022"
    Entonces el sistema rechaza el cambio e indica que el ID Registro ya está en uso

  Escenario: Vincular la Causa a una ya existente por Pieza Sumarial
    Dado que tengo abierto un informe en estado Borrador
    Y ya existe una Causa con N° de Pieza Sumarial "7070029/26"
    Cuando completo la Causa del informe con Pieza Sumarial "7070029/26"
    Entonces el informe queda vinculado a la Causa existente
    Y no se crea una Causa nueva

  Escenario: Sugerir Causas existentes cuando la Pieza Sumarial no matchea ninguna
    Dado que tengo abierto un informe en estado Borrador
    Y estoy completando el campo Causa
    Cuando el N° de Pieza Sumarial que ingreso no coincide exactamente con
      ninguna Causa existente
    Entonces el sistema me muestra las Causas existentes con carátula
      parecida como sugerencia
    Y puedo elegir una de esas sugerencias en vez de crear una Causa nueva

  Escenario: Completar la Causa sin Circunscripción judicial al editar
    Dado que tengo abierto un informe en estado Borrador
    Y completo Carátula y N° de Pieza Sumarial de la Causa
    Cuando dejo la Circunscripción judicial sin especificar
    Entonces la Causa se crea o vincula igual, sin Circunscripción judicial

  Escenario: Completar solo la Carátula, sin N° de Pieza Sumarial
    Dado que tengo abierto un informe en estado Borrador cuya Dependencia
      no aporta N° de Pieza Sumarial (ej. Narcotráfico)
    Cuando completo la Carátula de la Causa y dejo el N° de Pieza
      Sumarial vacío
    Entonces la Causa se crea igual, con Carátula y sin N° de Pieza
      Sumarial
    Y no se reutiliza la Causa de ningún otro informe que también esté
      sin N° de Pieza Sumarial

  Escenario: Dos informes distintos sin N° de Pieza Sumarial no comparten Causa
    Dado que el informe "38/2023" y el informe "73/2022" tienen
      Carátulas distintas y ninguno tiene N° de Pieza Sumarial
    Cuando completo la Causa de cada uno por separado
    Entonces cada informe queda vinculado a su propia Causa, con su
      propia Carátula
    Y ninguno de los dos pisa la Carátula del otro

  Escenario: Vincular Vehículos y Personas sin salir de la edición
    Dado que tengo abierto un informe en estado Borrador para editar
    Cuando busco un Vehículo por dominio o una Persona por DNI desde la
      pantalla de edición
    Y confirmo la vinculación
    Entonces el Vehículo o la Persona queda vinculado al informe
    Y no tengo que guardar los cambios y volver al Detalle para vincularlo

  Escenario: Crear un Vehículo nuevo sin salir de la edición
    Dado que tengo abierto un informe en estado Borrador para editar
    Y el Vehículo real no existe todavía en el catálogo
    Cuando lo creo desde el atajo "+ Nuevo Vehículo" de la pantalla de edición
    Entonces el Vehículo queda creado y vinculado automáticamente al informe
    Y no tengo que interrumpir la edición del informe en curso

  Escenario: Rechazar un Vehículo nuevo con el mismo Dominio que uno existente
    Dado que ya existe un Vehículo con Dominio "IAK796"
    Cuando intento crear otro Vehículo con el mismo Dominio "IAK796" desde
      el atajo de la pantalla de edición
    Entonces el sistema rechaza el alta y me indica que el Dominio ya existe

  Escenario: Crear varios Vehículos sin Dominio identificado
    Dado que ya existe un Vehículo sin Dominio (sin identificar)
    Cuando creo otro Vehículo también sin Dominio desde el atajo
    Entonces el sistema permite el alta, porque no hay Dominio concreto
      que pueda estar duplicado

  Escenario: Crear una Persona nueva sin salir de la edición
    Dado que tengo abierto un informe en estado Borrador para editar
    Y la Persona real no existe todavía en el catálogo
    Cuando la creo desde el atajo "+ Nueva Persona" de la pantalla de edición
    Entonces la Persona queda creada y vinculada automáticamente al informe
    Y no tengo que interrumpir la edición del informe en curso

  Escenario: Rechazar una Persona nueva con el mismo DNI que una existente
    Dado que ya existe una Persona con DNI "30111222"
    Cuando intento crear otra Persona con el mismo DNI "30111222" desde
      el atajo de la pantalla de edición
    Entonces el sistema rechaza el alta y me indica que el DNI ya existe

  Escenario: Crear varias Personas sin identificar
    Dado que ya existe una Persona sin DNI (sin identificar, solo con
      características descriptivas)
    Cuando creo otra Persona también sin DNI desde el atajo
    Entonces el sistema permite el alta, porque no hay DNI concreto que
      pueda estar duplicado
```

### Notas de modelado

- No se agrega ninguna entidad nueva. `RegistrarVehiculoCommandHandler` y
  `RegistrarPersonaCommandHandler` ganan un chequeo `AnyAsync` por
  `Dominio`/`Dni` **solo cuando el valor viene completo** — ambos campos
  siguen siendo opcionales en `Vehiculo`/`Persona` (un Vehículo/Persona
  sin identificar puede coexistir con otros también sin identificar, sin
  disparar el rechazo). Rechazo vía `EntidadDuplicadaException`, mismo
  patrón ya usado para `Dependencia.Nombre`/`TipoCausa.Nombre`.
- El atajo "+ Nuevo Vehículo"/"+ Nueva Persona" en `Editar.razor` reusa
  exactamente los mismos campos que las páginas de alta ya existentes
  (`/vehiculos/nuevo`, `/personas/nueva`) — mismo patrón inline ya usado
  para "+ Nueva Dependencia" (HU-02) y "+ Nuevo Tipo de Causa" (HU-19):
  panel desplegable, sin salir de la edición. Al guardar, crea el
  Vehículo/Persona y lo vincula al Informe en una sola operación (dos
  Commands en secuencia desde el componente: `RegistrarVehiculoCommand`/
  `RegistrarPersonaCommand` seguido de
  `VincularVehiculoInformeCommand`/`VincularPersonaInformeCommand`, que ya
  es idempotente si por algún motivo se reintenta).
- No se agrega validación de duplicado en ningún otro punto de alta de
  Vehículo/Persona fuera de este atajo (ej. la página `/vehiculos/nuevo`
  ya existente) — se aplica en el Handler, no en la UI, así que cubre
  automáticamente todos los caminos que llaman al mismo Command.
- El atajo hace dos Commands en secuencia (crear, después vincular) — si
  el primero (crear) falla, el mensaje de error es el esperable ("no se
  pudo crear"). Si el primero tiene éxito pero el segundo (vincular)
  falla (ej. el Informe pasó a `Publicado` en el medio), el Vehículo/
  Persona **ya quedó creado y persistido** — el mensaje de error lo
  aclara explícitamente en vez de decir "no se pudo crear" (que sería
  falso y podría llevar a reintentar, creando un duplicado real), e
  indica buscarlo por Dominio/DNI para vincularlo manualmente. Hallazgo
  del security-reviewer.

---

## HU-03 · Publicar / firmar un informe

**Como** Analista firmante
**Quiero** marcar un informe como definitivo (Publicado)
**Para** que quede visible en búsquedas generales y ya no admita ediciones libres

```gherkin
Característica: Publicación de informe

  Escenario: Publicación exitosa
    Dado que el informe tiene todos los campos obligatorios completos
    Y tengo asignado el rol de Analista Firmante en ese informe
    Cuando publico el informe
    Entonces su estado cambia a "Publicado"
    Y queda disponible en las búsquedas para todo el equipo

  Escenario: Intento de publicar sin campos obligatorios
    Dado que falta la Causa o la Dependencia destino
    Cuando intento publicar
    Entonces el sistema rechaza la acción y me indica qué falta completar
```

---

## HU-04 · Migración histórica de informes desde Drive

**Como** Administrador
**Quiero** ejecutar una migración masiva de los PDFs históricos
**Para** tener el histórico completo (500-5000 informes) indexado sin cargarlo informe por informe

```gherkin
Característica: Migración masiva

  Escenario: Migración por lote
    Dado que tengo una carpeta con PDFs históricos exportados de Drive
    Cuando ejecuto el proceso de migración
    Entonces el sistema procesa cada PDF con el mismo extractor que la carga individual
    Y genera un reporte final con: total procesados, exitosos, con advertencias
      (campos no reconocidos) y fallidos (PDF no legible)
    Para que el equipo revise manualmente los casos con advertencias o fallos

  Escenario: PDF con Fecha de Análisis no reconocida queda pendiente, no se pierde
    Dado que un PDF del lote tiene ID Registro reconocido pero Fecha de
      Análisis no reconocida
    Cuando se procesa ese PDF durante la migración
    Entonces el sistema guarda el PDF y los demás datos ya extraídos como
      una Migración Pendiente, en vez de descartar el archivo
    Y el reporte del lote lo marca "Con advertencia" con un enlace para
      completarlo

  Escenario: Completar la Fecha de Análisis de una Migración Pendiente
    Dado que existe una Migración Pendiente por Fecha de Análisis no reconocida
    Cuando el Administrador ingresa la fecha correcta desde
      /informes/migrar/pendientes
    Entonces se crea el Informe real con los datos ya extraídos más la
      fecha ingresada
    Y la Migración Pendiente deja de listarse

  Escenario: Ver el PDF original al completar una Migración Pendiente
    Dado que existe una Migración Pendiente por Fecha de Análisis y/o ID
      Registro no reconocidos
    Cuando el Administrador hace click en "Ver PDF" desde
      /informes/migrar/pendientes
    Entonces se muestra el PDF original guardado en la migración
    Para que pueda leer la fecha o el ID Registro directamente del
      documento en vez de completarlos a ciegas

  Escenario: PDF con ID Registro no reconocido también queda pendiente, no se pierde
    Dado que un PDF del lote no tiene ID Registro reconocido
    Cuando se procesa ese PDF durante la migración
    Entonces el sistema guarda el PDF y los demás datos ya extraídos como
      una Migración Pendiente sin ID Registro, en vez de descartar el archivo
    Y el reporte del lote lo marca "Con advertencia" con un enlace para
      completarlo

  Escenario: Completar el ID Registro de una Migración Pendiente
    Dado que existe una Migración Pendiente sin ID Registro reconocido
    Cuando el Administrador ingresa el ID Registro y, si también falta,
      la Fecha de Análisis desde /informes/migrar/pendientes
    Entonces se crea el Informe real con los datos ya extraídos más lo
      ingresado
    Y la Migración Pendiente deja de listarse

  Escenario: El ID Registro ingresado ya existe
    Dado que existe una Migración Pendiente sin ID Registro reconocido
    Cuando el Administrador ingresa un ID Registro que ya pertenece a
      otro Informe
    Entonces el sistema rechaza la operación sin crear el Informe
    Y la Migración Pendiente sigue listada para corregir el dato

  Escenario: Un PDF migrado se vincula automáticamente a una Causa ya existente
    Dado que el parser reconoce Carátula y N° de Pieza Sumarial en un PDF del lote
    Y ya existe una Causa con ese mismo N° de Pieza Sumarial exacto
    Cuando se procesa ese PDF durante la migración (exitosa o al
      completar una Migración Pendiente)
    Entonces el Informe creado queda vinculado a la Causa existente
    Y no se crea una Causa nueva

  Escenario: Un PDF migrado sin match exacto de Causa queda sin Causa asociada
    Dado que el parser reconoce Carátula y N° de Pieza Sumarial en un PDF del lote
    Y ningún N° de Pieza Sumarial existente coincide exacto
    Cuando se procesa ese PDF durante la migración
    Entonces el Informe se crea sin Causa asociada, igual que si el
      parser no hubiera reconocido esos campos
    Para que se complete después desde la edición manual (HU-02), donde
      sí se sugieren Causas parecidas
```

### Notas técnicas
- Los informes migrados entran en estado **Borrador** (no Publicado)
  hasta que un analista los revise, salvo que el equipo decida un flag de
  "migración validada en bloque" para acelerar el proceso (a definir).
- **Migración Pendiente** (ver `01-glosario-dominio.md`,
  `03-modelo-dominio.md`) es la entidad que sostiene estos cuatro
  escenarios — se crea tanto si falta la Fecha de Análisis como si falta
  el ID Registro (o ambos), a diferencia del diseño inicial (HU-04, primer
  corte) donde solo cubría la falta de fecha. El único caso que sigue sin
  persistir nada es un PDF ilegible/corrupto (`Fallido`, no
  `ConAdvertencia`) — ahí no hay datos que guardar.
