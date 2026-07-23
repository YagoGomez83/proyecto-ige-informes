# Épica 04 · Gestión de Catálogos (Dependencias, Cámaras, Barrios)

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

  Escenario: Código duplicado
    Dado que ya existe una Cámara con el código "SL 18"
    Cuando intento dar de alta otra Cámara con el mismo código
    Entonces el sistema rechaza el alta y me indica que el código ya existe
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
