# Épica 02 · Búsqueda y Analítica

## HU-05 · Buscar informes por múltiples criterios

**Como** Analista
**Quiero** buscar informes por causa, dependencia, dominio de vehículo, DNI/nombre
de persona o texto libre del relato
**Para** encontrar antecedentes relevantes en segundos

```gherkin
Característica: Búsqueda de informes

  Escenario: Búsqueda por dominio de vehículo
    Dado que existen informes con vehículos registrados
    Cuando busco por el dominio "IAK796"
    Entonces obtengo todos los informes donde ese vehículo fue mencionado
      o documentado en alguna evidencia

  Escenario: Búsqueda por texto libre en el relato
    Dado que busco la palabra "narcotráfico"
    Entonces el sistema devuelve informes donde esa palabra aparece en el
      relato, ordenados por relevancia

  Escenario: Combinación de filtros
    Dado que filtro por Dependencia = "Comisaría Seccional Primera"
      y rango de fechas = último trimestre
    Entonces obtengo solo los informes que cumplen ambas condiciones
```

---

## HU-06 · Tablero de analítica de gestión

**Como** Supervisor de Equipo Analítica
**Quiero** ver cantidad de informes por dependencia, por causa/tipo de causa
y por analista, en un rango de fechas
**Para** reportar la carga de trabajo del equipo sin armar planillas a mano

```gherkin
Característica: Tablero de analítica

  Escenario: Reporte por dependencia
    Dado que selecciono un rango de fechas
    Cuando abro el tablero de "Informes por Dependencia"
    Entonces veo un gráfico y tabla con el conteo de informes por cada
      dependencia solicitante en ese período

  Escenario: Exportar reporte
    Dado que estoy viendo cualquier tablero de analítica
    Cuando presiono "Exportar"
    Entonces puedo descargar los datos en Excel/CSV
```

---

## HU-07 · Ficha 360° de un vehículo o persona

**Como** Analista
**Quiero** ver una ficha consolidada de un vehículo o persona con todos los
informes/evidencias donde aparece
**Para** entender el historial completo sin abrir informe por informe

```gherkin
Característica: Ficha consolidada

  Escenario: Ver historial de un vehículo
    Dado que abro la ficha del vehículo con dominio "MRK064"
    Entonces veo: datos del vehículo, su estado actual, y la lista de todos
      los informes/evidencias donde aparece, ordenados cronológicamente
```
