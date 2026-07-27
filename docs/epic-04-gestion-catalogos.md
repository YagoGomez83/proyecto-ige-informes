# Épica 04 · Gestión de Catálogos (Dependencias, Cámaras, Barrios, Localidades, Centros de Control)

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
**Quiero** mantener un catálogo de Barrios
**Para** reutilizarlos como jurisdicción geográfica de distintas Dependencias sin
duplicar nombres escritos de forma distinta

```gherkin
Característica: Catálogo de Barrios

  Escenario: Alta de un Barrio
    Dado que completo el nombre de un Barrio nuevo
    Cuando confirmo el alta
    Entonces queda disponible para asignarse a cualquier Dependencia

  Escenario: Nombre duplicado
    Dado que ya existe un Barrio llamado "Barrio Norte"
    Cuando intento dar de alta otro Barrio con el mismo nombre
    Entonces el sistema rechaza el alta y me indica que el nombre ya existe
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
```

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
