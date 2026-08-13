# Épica 04 · Gestión de Catálogos (Dependencias, Cámaras, Barrios, Localidades, Centros de Control, Tipos de Incidente)

## HU-11 · Alta y jurisdicción geográfica de Dependencias

**Como** Administrador
**Quiero** dar de alta una Dependencia y, si corresponde, asignarle los Barrios
de su jurisdicción geográfica
**Para** mantener el catálogo de organismos externos actualizado y saber qué
Comisaría cubre cada zona

```gherkin
Característica: Gestión de Dependencias

  Escenario: Alta de una Dependencia
    Dado que completo nombre y tipo de una Dependencia nueva
    Cuando confirmo el alta
    Entonces queda disponible en el catálogo para asignarse a Casos e Informes

  Escenario: Nombre duplicado
    Dado que ya existe una Dependencia con el nombre "Comisaría Seccional Primera"
    Cuando intento dar de alta otra Dependencia con el mismo nombre
    Entonces el sistema rechaza el alta y me indica que el nombre ya existe

  Escenario: Asignar jurisdicción geográfica
    Dado que doy de alta una Comisaría
    Cuando le asigno uno o más Barrios de su jurisdicción
    Entonces esos Barrios quedan visibles en la ficha de la Dependencia

  Escenario: Dependencia sin jurisdicción geográfica
    Dado que doy de alta una Fiscalía
    Cuando no le asigno ningún Barrio
    Entonces la Dependencia queda creada igual, sin jurisdicción geográfica
```

### Eliminar una Dependencia sin uso (extensión de HU-11)

> Contexto (2026-08-12): al probar el atajo "+ Nueva Dependencia" desde
> Editar Informe se crearon por error dos Dependencias parecidas
> ("Departamento Investigaciones" y "División Investigaciones"). No existía
> ningún mecanismo para sacar la duplicada sin uso salvo tocar la base de
> datos a mano — esta extensión cierra ese gap con un alta controlada
> (mismo criterio de auditoría y autorización que el resto de HU-11).

```gherkin
  Escenario: Eliminar una Dependencia sin uso
    Dado que existe una Dependencia sin ningún Informe, Cámara, Barrio
    asignado, ni otra Dependencia que la tenga como Unidad Regional
    Cuando un Administrador la elimina desde su ficha
    Entonces la Dependencia deja de existir en el catálogo

  Escenario: No se puede eliminar una Dependencia referenciada por un Informe
    Dado que una Dependencia es la Dependencia Destino de al menos un Informe
    Cuando un Administrador intenta eliminarla
    Entonces el sistema rechaza la eliminación e indica que está en uso

  Escenario: No se puede eliminar una Dependencia referenciada por una Cámara
    Dado que una Dependencia tiene al menos una Cámara asignada
    Cuando un Administrador intenta eliminarla
    Entonces el sistema rechaza la eliminación e indica que está en uso

  Escenario: No se puede eliminar una Dependencia referenciada por un Caso de Análisis
    Dado que una Dependencia es la Dependencia de al menos un Caso de Análisis
    Cuando un Administrador intenta eliminarla
    Entonces el sistema rechaza la eliminación e indica que está en uso

  Escenario: No se puede eliminar una Dependencia referenciada por una Migración Pendiente
    Dado que una Dependencia es la Dependencia Destino de al menos una
    Migración Pendiente sin completar todavía
    Cuando un Administrador intenta eliminarla
    Entonces el sistema rechaza la eliminación e indica que está en uso

  Escenario: No se puede eliminar una Dependencia que es Unidad Regional de otra
    Dado que una Dependencia de tipo "UnidadRegional" tiene al menos una
    Comisaría con ese UnidadRegionalId asignado
    Cuando un Administrador intenta eliminarla
    Entonces el sistema rechaza la eliminación e indica que está en uso
```

### Notas de modelado

- El Handler valida en `Application` (no hay `ON DELETE RESTRICT` a nivel
  de base para `CasoAnalisis.DependenciaId` ni
  `MigracionPendiente.DependenciaDestinoId` — solo `Camara.DependenciaId`
  tiene FK real configurada en `CamaraConfiguration`; ver hallazgo de
  `security-reviewer` del 2026-08-12). Se optó por chequeo explícito en el
  Handler para las cuatro entidades que referencian `Dependencia`
  (`Informe`, `Camara`, `CasoAnalisis`, `MigracionPendiente`) más la
  propia `Dependencia` vía `UnidadRegionalId`, en vez de agregar FKs a
  nivel de base — mantiene el chequeo centralizado en un solo lugar y
  evita tocar migraciones de columnas ya en producción.
- No hay transacción explícita entre las validaciones (`AnyAsync`) y el
  borrado (`Remove` + `SaveChangesAsync`) — ver riesgo aceptado en
  `docs/06-seguridad-amenazas.md`, sección de Tampering.

---

## HU-12 · Alta manual de Cámaras con Dependencia opcional

**Como** Administrador
**Quiero** dar de alta una Cámara manualmente y asignarle opcionalmente la
Dependencia en cuya jurisdicción se encuentra
**Para** completar el catálogo sin depender de que aparezca primero en un PDF

```gherkin
Característica: Alta manual de Cámaras

  Escenario: Alta de una Cámara Domo dentro de una jurisdicción
    Dado que completo código, tipo "Domo" y una Dependencia existente
    Cuando confirmo el alta
    Entonces la Cámara queda creada y vinculada a esa Dependencia

  Escenario: Alta de una Cámara LPR sin Dependencia
    Dado que completo código y tipo "LPR" para una cámara en ruta
    Cuando no selecciono ninguna Dependencia
    Entonces la Cámara queda creada sin jurisdicción asociada

  Escenario: Código repetido entre cámaras de una misma instalación
    Dado que ya existe una Cámara con el código "PLI"
    Cuando doy de alta otra Cámara con el mismo código "PLI" pero distinta Ubicación
    Entonces el sistema permite el alta, ya que el código no es único (una
    instalación agrupada, ej. un peaje, puede tener varias cámaras bajo el
    mismo código)

  Escenario: Alta de Cámara con Localidad y Centro de Control
    Dado que completo código, tipo, y selecciono una Localidad y un Centro
    de Control de Cámaras existentes
    Cuando confirmo el alta
    Entonces la Cámara queda creada con esa Localidad y ese Centro de
    Control asociados

  Escenario: Alta de Cámara sin Localidad ni Centro de Control
    Dado que no selecciono Localidad ni Centro de Control de Cámaras
    Cuando confirmo el alta
    Entonces la Cámara queda creada igual, con esos campos vacíos
```

---

## HU-13 · Catálogo de Barrios

**Como** Administrador
**Quiero** mantener un catálogo de Barrios, asociado opcionalmente a la
Localidad donde está
**Para** reutilizarlos como jurisdicción geográfica de distintas Dependencias
sin duplicar nombres escritos de forma distinta, y sin que un mismo nombre de
Barrio en dos ciudades distintas (ej. "Barrio Norte" en San Luis y en Villa
Mercedes) choquen entre sí

> Extensión (2026-07-29): originalmente `Barrio.Nombre` era único a nivel
> global, sin relación con `Localidad`. Se detectó que dos ciudades distintas
> pueden tener un barrio con el mismo nombre — la unicidad pasa a ser por
> combinación (Nombre, Localidad), no por Nombre solo.

```gherkin
Característica: Catálogo de Barrios

  Escenario: Alta de un Barrio con Localidad
    Dado que completo el nombre de un Barrio nuevo y selecciono una Localidad existente
    Cuando confirmo el alta
    Entonces queda disponible para asignarse a cualquier Dependencia, con esa Localidad asociada

  Escenario: Alta de un Barrio sin Localidad
    Dado que completo el nombre de un Barrio nuevo y no selecciono ninguna Localidad
    Cuando confirmo el alta
    Entonces el Barrio queda creado igual, sin Localidad asociada

  Escenario: Nombre duplicado dentro de la misma Localidad
    Dado que ya existe un Barrio llamado "Barrio Norte" en la Localidad "San Luis"
    Cuando intento dar de alta otro Barrio con el mismo nombre en la misma Localidad "San Luis"
    Entonces el sistema rechaza el alta y me indica que el nombre ya existe en esa Localidad

  Escenario: Mismo nombre en Localidades distintas
    Dado que ya existe un Barrio llamado "Barrio Norte" en la Localidad "San Luis"
    Cuando doy de alta otro Barrio llamado "Barrio Norte" en la Localidad "Villa Mercedes"
    Entonces el sistema permite el alta, ya que son Localidades distintas

  Escenario: Mismo nombre sin Localidad en ambos casos
    Dado que ya existe un Barrio llamado "Barrio Norte" sin Localidad asociada
    Cuando doy de alta otro Barrio llamado "Barrio Norte" tampoco sin Localidad asociada
    Entonces el sistema permite el alta, porque todavía no se sabe la Localidad
    de ninguno de los dos y no se puede garantizar que sean distintos ni que
    sean el mismo
```

---

## HU-14 · Catálogo de Localidades

**Como** Administrador
**Quiero** mantener un catálogo de Localidades
**Para** registrar en qué ciudad, pueblo o paraje está físicamente instalada
cada Cámara, sin duplicar nombres escritos de forma distinta

> Nota: `Localidad` es un catálogo geográfico distinto de `Barrio` — ver
> `docs/01-glosario-dominio.md`. `Barrio` es la jurisdicción de una
> `Dependencia`; `Localidad` es un atributo de dónde está una `Camara`.
> Desde HU-13, `Barrio` puede asociarse opcionalmente a una `Localidad`
> (`Barrio.LocalidadId`) — no como jerarquía formal, solo para distinguir
> Barrios homónimos en ciudades distintas. El listado de Localidades
> muestra cuántos Barrios referencian a cada una, como dato informativo
> para el Administrador (no bloquea ni condiciona el alta/uso).

```gherkin
Característica: Catálogo de Localidades

  Escenario: Alta de una Localidad
    Dado que completo el nombre de una Localidad nueva
    Cuando confirmo el alta
    Entonces queda disponible para asignarse a cualquier Cámara

  Escenario: Nombre duplicado
    Dado que ya existe una Localidad llamada "Estancia Grande"
    Cuando intento dar de alta otra Localidad con el mismo nombre
    Entonces el sistema rechaza el alta y me indica que el nombre ya existe

  Escenario: Listado muestra Barrios asociados
    Dado que la Localidad "San Luis" tiene 2 Barrios con LocalidadId
    apuntando a ella
    Cuando veo el listado de Localidades
    Entonces la fila de "San Luis" indica "2 Barrios"

  Escenario: Localidad sin Barrios asociados todavía
    Dado que la Localidad "Potrero de Los Funes" no tiene ningún Barrio
    con LocalidadId apuntando a ella
    Cuando veo el listado de Localidades
    Entonces la fila de "Potrero de Los Funes" lo indica visualmente
    (ej. "Sin Barrios asociados"), distinto de un dato numérico
```

---

## HU-15 · Catálogo de Centros de Control de Cámaras

**Como** Administrador
**Quiero** mantener un catálogo de Centros de Control de Cámaras (CCC)
**Para** saber qué centro monitorea cada Cámara del sistema

```gherkin
Característica: Catálogo de Centros de Control de Cámaras

  Escenario: Alta de un Centro de Control de Cámaras
    Dado que completo sigla "CCCSL" y nombre "Centro de Control de Cámaras San Luis"
    Cuando confirmo el alta
    Entonces queda disponible para asignarse a cualquier Cámara

  Escenario: Sigla duplicada
    Dado que ya existe un Centro de Control de Cámaras con sigla "CCCSL"
    Cuando intento dar de alta otro con la misma sigla
    Entonces el sistema rechaza el alta y me indica que la sigla ya existe
```

---

## HU-16 · Jerarquía de Unidad Regional sobre Comisarías

**Como** Administrador
**Quiero** asignarle a una Dependencia de tipo Comisaría la Unidad Regional
de la que depende
**Para** reflejar que una Unidad Regional agrupa varias Comisarías/Jurisdicciones

```gherkin
Característica: Jerarquía de Unidad Regional

  Escenario: Asignar Unidad Regional a una Comisaría
    Dado que doy de alta o edito una Dependencia de tipo "Comisaria"
    Cuando le asigno como Unidad Regional una Dependencia existente de tipo "UnidadRegional"
    Entonces la Comisaría queda agrupada bajo esa Unidad Regional

  Escenario: No se puede asignar una Unidad Regional inválida
    Dado que intento asignar como Unidad Regional una Dependencia de tipo "Fiscalia"
    Cuando confirmo la asignación
    Entonces el sistema rechaza la operación e indica que solo se puede
    asignar una Dependencia de tipo "UnidadRegional"

  Escenario: Comisaría sin Unidad Regional asignada
    Dado que doy de alta una Comisaría
    Cuando no le asigno ninguna Unidad Regional
    Entonces la Dependencia queda creada igual, sin Unidad Regional asociada

  Escenario: Ver las Comisarías de una Unidad Regional
    Dado que existen varias Comisarías con la misma Unidad Regional asignada
    Cuando consulto la ficha de esa Unidad Regional
    Entonces veo listadas todas las Comisarías que dependen de ella
```

---

## HU-18 · Catálogo de Tipos de Incidente

**Como** Administrador
**Quiero** mantener un catálogo de Tipos de Incidente (código + descripción)
**Para** clasificar los Casos de Análisis con los códigos operativos reales
(ej. "25 - Persona sospechosa", "02 - Asalto a mano armada"), sin depender de
cargarlos a mano en la base de datos

> Contexto: el modelo `TipoIncidente` y su listado ya existían desde la Fase 1
> (`ObtenerCasoPorIdQueryHandler`, `RegistrarCasoCommand`, etc. ya lo
> consumen), pero no existía ningún Command de alta ni página de gestión —
> los únicos registros eran cargados a mano en la base para pruebas. Esta HU
> cierra ese gap siguiendo el mismo patrón que HU-13 (Barrios).

```gherkin
Característica: Catálogo de Tipos de Incidente

  Escenario: Alta de un Tipo de Incidente
    Dado que completo el código "25" y la descripción "Persona sospechosa"
    Cuando confirmo el alta
    Entonces queda disponible para asignarse a cualquier Caso de Análisis

  Escenario: Código duplicado
    Dado que ya existe un Tipo de Incidente con el código "25"
    Cuando intento dar de alta otro Tipo de Incidente con el mismo código
    Entonces el sistema rechaza el alta y me indica que el código ya existe
```

### Notas de modelado

- No es una entidad nueva — `TipoIncidente` (`Codigo` + `Descripcion`) ya
  existe en `Domain` desde la Fase 1, con índice único por `Codigo` ya
  configurado en `TipoIncidenteConfiguration`. Esta HU solo agrega el Command
  de escritura (`CrearTipoIncidenteCommand`) y la UI, reutilizando el Query
  de listado (`ListarTiposIncidenteQuery`) que ya existía.
- Mismo criterio de auditoría que Barrio/Localidad/CCC: catálogo de baja
  sensibilidad sin PII, sin auditoría de alta/lectura.
- `[Autorizar(Roles.Admin)]` en el Command, página en `/configuracion` junto
  al resto de los catálogos.
- Carga masiva de los códigos históricos reales: pendiente de que se
  consiga el listado completo (ver deuda en `08-plan-implementacion.md`) —
  esta HU deja el mecanismo de alta manual listo para cargarlos uno por uno
  o para un futuro importador si el volumen lo justifica.

---

## HU-19 · Catálogo de Tipos de Causa

**Como** Administrador
**Quiero** mantener un catálogo preseteado de carátulas de Causa (Tipo de
Causa)
**Para** que el Analista elija la carátula de un `<select>` al editar un
Informe, en vez de tipearla como texto libre cada vez

> Contexto: el usuario reportó que tipear la Carátula a mano en cada
> edición de Informe generaba inconsistencias de transcripción — mismo
> problema que ya había motivado matchear `Causa` por N° de Pieza
> Sumarial en vez de por carátula (ver HU-02, docs/03-modelo-dominio.md
> "Decisiones ya resueltas"). `TipoCausa` no reemplaza a `Causa`: el N°
> de pieza sumarial y la circunscripción judicial siguen siendo
> específicos de cada expediente y se completan a mano; solo la
> Carátula pasa a elegirse de un catálogo preseteado.

```gherkin
Característica: Catálogo de Tipos de Causa

  Escenario: Alta de un Tipo de Causa
    Dado que completo el nombre "AV. ROBO CALIFICADO"
    Cuando confirmo el alta
    Entonces queda disponible en el selector de Carátula al editar cualquier Informe

  Escenario: Nombre duplicado
    Dado que ya existe un Tipo de Causa con el nombre "AV. ROBO"
    Cuando intento dar de alta otro Tipo de Causa con el mismo nombre
    Entonces el sistema rechaza el alta y me indica que el nombre ya existe

  Escenario: Agregar un Tipo de Causa faltante sin salir de la edición del Informe
    Dado que soy Administrador y estoy editando un Informe
    Y el Tipo de Causa real no está en el catálogo
    Cuando lo creo desde el atajo de la pantalla de edición
    Entonces queda disponible en el selector y seleccionado automáticamente
    Para que no tenga que interrumpir la edición del Informe en curso
```

### Notas de modelado

- Entidad nueva `TipoCausa` (`Nombre` único, sin más campos) —
  `src/IGE.Informes.Domain/Entities/TipoCausa.cs`.
- `TipoCausa` **no es una FK desde `Causa`** — `Causa.Caratula` sigue
  siendo `string`, el `<select>` solo alimenta el valor de texto al
  crear/editar la Causa del Informe (mismo campo de siempre). No rompe
  el matching por N° de Pieza Sumarial ya implementado.
- Mismo criterio de auditoría que Barrio/Localidad/CCC/TipoIncidente:
  catálogo de baja sensibilidad sin PII, sin auditoría explícita de
  alta/lectura (el alta sí queda cubierta por el `AuditLogInterceptor`
  genérico, como toda entidad `IAuditable`).
- `[Autorizar(Roles.Admin)]` en el Command de alta. Página de listado en
  `/configuracion` junto al resto de los catálogos; atajo de alta
  también embebido en `/informes/{id}/editar` (mismo patrón ya usado
  para `Dependencia`).
- Migración inicial del catálogo histórico real: 82 carátulas activas
  desde `docs/docuemntación-legacy/causes.csv` (ver
  docs/03-modelo-dominio.md — ese archivo **no** es la deuda pendiente
  de `TipoIncidente`, es un catálogo distinto a pesar del nombre).

---

## HU-20 · Catálogo de Marca y Color de Vehículo

**Como** Administrador
**Quiero** mantener catálogos preseteados de Marca y Color de Vehículo,
con opción de tipear un valor libre si el real no está en la lista
**Para** que dar de alta un Vehículo sea más rápido y consistente, sin
perder la posibilidad de cargar una marca/color poco común

> Contexto: el usuario reportó que tipear Marca y Color a mano en cada
> alta de Vehículo generaba muchas variaciones del mismo dato real (ej.
> "VW", "VOLKSWAGEN", "VOLKWAGEN", "VOLSWAGEN", "WV" para la misma marca
> — confirmado contando los valores reales ya cargados en producción).
> Mismo problema de fondo que ya motivó `TipoCausa` (HU-19) para la
> Carátula de Causa — mismo criterio de solución: catálogo preseteado
> con `<select>`, más un atajo de alta para no bloquear un caso real que
> no esté todavía en la lista.

```gherkin
Característica: Catálogo de Marca de Vehículo

  Escenario: Alta de una Marca
    Dado que completo el nombre "Chevrolet"
    Cuando confirmo el alta
    Entonces queda disponible en el selector de Marca al registrar cualquier Vehículo

  Escenario: Nombre duplicado
    Dado que ya existe una Marca con el nombre "Ford"
    Cuando intento dar de alta otra Marca con el mismo nombre
    Entonces el sistema rechaza el alta y me indica que el nombre ya existe

Característica: Catálogo de Color de Vehículo

  Escenario: Alta de un Color
    Dado que completo el nombre "Gris Oscuro"
    Cuando confirmo el alta
    Entonces queda disponible en el selector de Color al registrar cualquier Vehículo

  Escenario: Nombre duplicado
    Dado que ya existe un Color con el nombre "Blanco"
    Cuando intento dar de alta otro Color con el mismo nombre
    Entonces el sistema rechaza el alta y me indica que el nombre ya existe

Característica: Elegir Marca/Color al registrar un Vehículo

  Escenario: Elegir una Marca del catálogo
    Dado que estoy registrando un Vehículo nuevo
    Cuando elijo "Toyota" del selector de Marca
    Entonces el Vehículo queda registrado con Marca "Toyota"

  Escenario: La Marca real no está en el catálogo
    Dado que estoy registrando un Vehículo nuevo
    Y la Marca real no está en el selector
    Cuando elijo la opción "Otra (especificar)" y tipeo "Hyundai"
    Entonces el Vehículo queda registrado con Marca "Hyundai" como texto
    libre, sin necesidad de agregarla antes al catálogo

  Escenario: Agregar una Marca faltante sin salir del alta del Vehículo
    Dado que soy Administrador y estoy registrando un Vehículo
    Y la Marca real no está en el catálogo
    Cuando la creo desde el atajo de la pantalla de alta
    Entonces queda disponible en el selector y seleccionada
    automáticamente, sin interrumpir la carga del Vehículo en curso

  Escenario: Mismo comportamiento para Color
    Dado que estoy registrando un Vehículo nuevo
    Cuando el Color real no está en el selector
    Entonces puedo elegir "Otra (especificar)" y tipearlo libremente,
    igual que con Marca
```

### Notas de modelado

- Dos entidades nuevas, mismo patrón que `TipoCausa`: `MarcaVehiculo`
  (`Nombre` único) y `ColorVehiculo` (`Nombre` único) —
  `src/IGE.Informes.Domain/Entities/MarcaVehiculo.cs` y
  `ColorVehiculo.cs`.
- **No son FK desde `Vehiculo`** — `Vehiculo.Marca` y `Vehiculo.Color`
  siguen siendo `string` (sin cambio de tipo ni migración de esas
  columnas). Los catálogos solo alimentan el `<select>` de la UI; el
  valor elegido (o tipeado libremente con "Otra") se copia como texto,
  mismo criterio que `TipoCausa` con `Causa.Caratula`.
- El histórico migrado (~1100 Vehículos, con Marca/Color ya cargados
  como texto libre desde antes de esta HU) **no se toca ni se
  normaliza** — sigue tal cual quedó migrado, decisión explícita del
  usuario. El catálogo nuevo aplica solo hacia adelante, para altas
  nuevas.
- `[Autorizar(Roles.Admin)]` en los Commands de alta de ambos catálogos.
  Páginas de listado en `/configuracion` junto al resto de los
  catálogos; atajo de alta embebido en `/vehiculos/nuevo` y en
  `/informes/{id}/editar` (atajo "+ Nuevo Vehículo", ver HU-02) — mismo
  patrón ya usado para `Dependencia` y `TipoCausa`.
- Catálogo base cargado manualmente por el Admin tras el despliegue (no
  vía importador de `IGE.Informes.DataMigration`, a diferencia de
  `TipoCausa` — acá son solo ~16 valores por catálogo, no justifica un
  importador de línea de comandos): marcas más comunes en Argentina
  (Chevrolet, Citroën, Fiat, Ford, Honda, Jeep, Nissan, Peugeot,
  Renault, Suzuki, Toyota, Volkswagen, más las motos más frecuentes en
  los datos reales — Motomel, Zanella, Corven, Bajaj); colores más
  comunes (Blanco, Negro, Gris, Gris Claro, Gris Oscuro, Rojo, Azul,
  Verde, Bordo, Celeste, Beige, Champagne, Crema, Plateado, Amarillo,
  Naranja).

---

## HU-17 · Gestión de Usuarios

**Como** Administrador
**Quiero** dar de alta usuarios nuevos, cambiarles el rol y bloquear/
desbloquear su acceso
**Para** administrar quién puede usar el sistema sin depender de AD/LDAP
(ver `00-vision-alcance.md`) ni de tocar la base de datos a mano

> Contexto: hasta esta HU, el único mecanismo de alta era `IdentitySeeder`
> (un único Admin creado por variable de entorno al primer arranque, ver
> `docs/09-onboarding-offboarding-usuarios.md`). Esta HU reemplaza el
> proceso manual documentado ahí por una UI real — actualizar ese documento
> una vez implementada.

```gherkin
Característica: Gestión de Usuarios

  Escenario: Alta de un usuario nuevo
    Dado que completo nombre completo, email, una contraseña de 12 o más
    caracteres, y selecciono el rol "Analista"
    Cuando confirmo el alta
    Entonces el usuario queda creado con ese rol y puede iniciar sesión
    con esa contraseña

  Escenario: Email duplicado
    Dado que ya existe un usuario con el email "ana.gomez@institucion.gob"
    Cuando intento dar de alta otro usuario con el mismo email
    Entonces el sistema rechaza el alta y me indica que el email ya existe

  Escenario: Contraseña que no cumple la política mínima
    Dado que completo una contraseña de menos de 12 caracteres
    Cuando confirmo el alta
    Entonces el sistema rechaza el alta e indica el motivo

  Escenario: Cambiar el rol de un usuario existente
    Dado que un usuario tiene el rol "Analista"
    Cuando le asigno el rol "Supervisor"
    Entonces el usuario pasa a tener el rol "Supervisor" y deja de tener "Analista"

  Escenario: Bloquear el acceso de un usuario
    Dado que un usuario activo existe en el sistema
    Cuando lo bloqueo
    Entonces no puede volver a iniciar sesión hasta que un Administrador lo desbloquee

  Escenario: Desbloquear el acceso de un usuario
    Dado que un usuario está bloqueado
    Cuando lo desbloqueo
    Entonces puede volver a iniciar sesión con su contraseña existente

  Escenario: Un Administrador no puede cambiarse el rol a sí mismo
    Dado que estoy logueado como Administrador
    Cuando intento cambiar mi propio rol desde la ficha de mi usuario
    Entonces el sistema rechaza la operación

  Escenario: Un Administrador no puede bloquearse a sí mismo
    Dado que estoy logueado como Administrador
    Cuando intento bloquear mi propio usuario
    Entonces el sistema rechaza la operación

  Escenario: Ver el listado de usuarios
    Dado que existen varios usuarios con distintos roles y estados
    Cuando accedo al listado de usuarios
    Entonces veo nombre, email, rol y si está bloqueado o activo, de cada uno

  Escenario: Un Administrador resetea la contraseña de otro usuario
    Dado que un usuario existente olvidó su contraseña
    Cuando ingreso una contraseña nueva de 12 o más caracteres desde su ficha
    y confirmo el reseteo
    Entonces el usuario puede iniciar sesión con la contraseña nueva y ya
    no con la anterior

  Escenario: Reseteo con contraseña que no cumple la política mínima
    Dado que estoy en la ficha de un usuario existente
    Cuando ingreso una contraseña nueva de menos de 12 caracteres y confirmo
    Entonces el sistema rechaza el reseteo e indica el motivo

  Escenario: Un Administrador no puede resetear su propia contraseña desde esta pantalla
    Dado que estoy logueado como Administrador
    Cuando intento resetear la contraseña de mi propio usuario
    Entonces el sistema rechaza la operación (para cambiar la propia
    contraseña ya existe "Cuenta > Administrar > Contraseña")

Característica: Cambio de la propia contraseña (extensión de HU-17)

  Escenario: Un usuario cambia su propia contraseña conociendo la actual
    Dado que estoy logueado con cualquier rol (Analista, Supervisor o Admin)
    Cuando voy a "Cuenta > Administrar > Contraseña", ingreso mi contraseña
    actual y una nueva de 12 o más caracteres, y confirmo
    Entonces puedo volver a iniciar sesión con la contraseña nueva y ya no
    con la anterior

  Escenario: Contraseña actual incorrecta
    Dado que estoy en "Cuenta > Administrar > Contraseña"
    Cuando ingreso una contraseña actual incorrecta
    Entonces el sistema rechaza el cambio e indica que la contraseña
    actual no coincide, sin revelar cuál es la correcta

  Escenario: Contraseña nueva que no cumple la política mínima
    Dado que estoy en "Cuenta > Administrar > Contraseña" con la
    contraseña actual correcta
    Cuando ingreso una contraseña nueva de menos de 12 caracteres
    Entonces el sistema rechaza el cambio e indica el motivo
```

### Reseteo de contraseña por un Admin (extensión de HU-17)

- El Admin tipea la contraseña nueva (mismo patrón que el alta: 12+
  caracteres, comunicada por un canal separado al usuario — ver
  `docs/09-onboarding-offboarding-usuarios.md`). No hay generación
  aleatoria ni envío de email (el sistema no tiene SMTP real, ver
  `IdentityNoOpEmailSender`).
- No se fuerza cambio de contraseña en el próximo login — alcance
  acotado a la resolución del reset en sí, igual de simple que el alta.
- Reutiliza el mismo tratamiento de auto-gestión que bloqueo/cambio de
  rol: un Admin no puede resetearse su propia contraseña desde esta
  pantalla (ya tiene su propio flujo en `Account/Manage/CambiarPassword`).
- Igual que `BloquearAsync`/`CambiarRolAsync`, invalida el `SecurityStamp`
  para cortar cualquier sesión Blazor ya abierta con la contraseña vieja
  (mismo mecanismo que `PersistingRevalidatingAuthenticationStateProvider`,
  ver `docs/06-seguridad-amenazas.md`).

### Cambio de la propia contraseña (extensión de HU-17)

- Abierto a los 3 roles (`CambiarPasswordPropiaCommand`,
  `[Autorizar(Analista, Supervisor, Admin)]`) — a diferencia del reseteo
  administrativo, requiere la contraseña actual (`UserManager.
  ChangePasswordAsync`, no el par `GeneratePasswordResetTokenAsync`/
  `ResetPasswordAsync` que usa el reseteo de Admin).
- El `UsuarioId` sobre el que opera sale siempre de
  `ICurrentUserService.UsuarioId` (el usuario autenticado), nunca de un
  parámetro del cliente — un usuario no puede cambiar la contraseña de
  otro por esta vía bajo ninguna circunstancia.
- `ChangePasswordAsync` invalida el `SecurityStamp` internamente al
  actualizar el hash — no hace falta el paso explícito de
  `UpdateSecurityStampAsync` que sí necesitan `CambiarRolAsync`/
  `BloquearAsync`/`ResetearPasswordAsync`.
- Página en `Account/Manage/CambiarPassword`, accesible desde el ícono de
  llave junto al usuario en el sidebar (`NavMenu.razor`).

### Perfil propio: nombre, rol y foto (extensión de HU-17)

**Como** Analista, Supervisor o Administrador
**Quiero** ver mi nombre y rol, y subir/cambiar mi foto de perfil
**Para** identificar visualmente mi cuenta en el sistema

```gherkin
Característica: Perfil propio

  Escenario: Ver los datos de mi perfil
    Dado que estoy logueado con cualquier rol
    Cuando voy a "Cuenta > Perfil"
    Entonces veo mi nombre completo, mi email y mi rol

  Escenario: Subir una foto de perfil por primera vez
    Dado que estoy en "Cuenta > Perfil" y no tengo foto cargada
    Cuando subo una imagen JPEG, PNG o WebP de hasta 10MB
    Entonces la foto queda visible en mi perfil, reemplazando el ícono de
    iniciales

  Escenario: Reemplazar una foto de perfil existente
    Dado que ya tengo una foto de perfil cargada
    Cuando subo una imagen nueva
    Entonces la foto anterior se reemplaza por la nueva y deja de estar
    disponible

  Escenario: Imagen rechazada por el escaneo antivirus
    Dado que subo un archivo que el escaneo antivirus detecta como amenaza
    Cuando confirmo la subida
    Entonces el sistema la rechaza y no queda ningún dato guardado
```

Notas de modelado:

- `ApplicationUser` gana `ImagenPerfilPath` (`string?`, nullable) — path
  crudo en MinIO, mismo `IFileStorage` que `VehiculoImagen`/
  `PersonaImagen`, nunca una URL directa persistida.
- A diferencia de `VehiculoImagen`/`PersonaImagen` (colección con
  histórico, una entidad `Domain` propia con `IAuditable`), acá es un
  campo único directo en `ApplicationUser` — no hay galería ni historial
  de fotos de perfil, cada subida reemplaza la anterior y la borra de
  MinIO (`SubirImagenPerfilCommandHandler`, mismo escaneo antivirus
  fail-closed y validación de magic bytes que el resto de las imágenes
  del sistema, ver `FormatoImagenHelper`).
- `SubirImagenPerfilCommand`/`ObtenerPerfilPropioQuery`
  (`[Autorizar(Analista, Supervisor, Admin)]`) operan siempre sobre
  `ICurrentUserService.UsuarioId`, mismo criterio que
  `CambiarPasswordPropiaCommand` — nunca reciben un `UsuarioId` del
  cliente, un usuario no puede subir/ver la foto de otro por esta vía.
- Página en `Account/Manage/Perfil`, accesible desde un ícono nuevo junto
  al usuario en el sidebar (`NavMenu.razor`), al lado del de "Cambiar
  contraseña".

### Notas de modelado

- No se agrega ninguna entidad nueva a `Domain` — esta HU opera sobre
  `ApplicationUser`/`ApplicationRole` de ASP.NET Core Identity
  (`Infrastructure/Identity`), fuera del modelo de dominio del negocio.
- `Application` no puede depender de `Infrastructure` (Clean Architecture,
  ver `CLAUDE.md`) ni de `UserManager<T>`/`RoleManager<T>` directamente —
  se agrega el puerto `IUserManagementService` en
  `Application/Common/Interfaces/`, implementado en `Infrastructure`
  envolviendo Identity real. Los Handlers dependen solo del puerto.
- Bloquear un usuario reutiliza el mecanismo de lockout ya usado contra
  fuerza bruta (`Identity.Lockout`, ver `docs/06-seguridad-amenazas.md`):
  `LockoutEnabled = true` + `LockoutEnd = DateTimeOffset.MaxValue`. No se
  borra el usuario nunca — el `AuditLog` referencia `UsuarioId` como FK
  (ver `docs/09-onboarding-offboarding-usuarios.md`).
- Un usuario tiene un único rol a la vez en este sistema (aunque Identity
  permite roles múltiples) — cambiar de rol remueve el anterior antes de
  asignar el nuevo, en la misma operación.
