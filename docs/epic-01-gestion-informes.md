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
```

### Notas técnicas
- El worker de extracción corre de forma asíncrona (background job); la UI
  muestra estado "Procesando" y notifica cuando está listo para revisión.
- Reutiliza el mismo parser que la migración histórica (ver HU-04).

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
```

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
