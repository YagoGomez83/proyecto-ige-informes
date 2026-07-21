# Épica 00 · Gestión de Casos de Análisis (núcleo diario)

> Esta épica reemplaza el uso de la planilla Excel de seguimiento diario.
> Es la entidad que más va a usar el equipo, más veces por día que la carga
> de Informes formales.

## HU-00 · Registrar un nuevo Caso de Análisis

**Como** Analista
**Quiero** cargar rápidamente un nuevo caso de análisis apenas me llega un pedido
**Para** no perder tiempo comparado con escribir la fila en Excel hoy

```gherkin
Característica: Alta de Caso de Análisis

  Escenario: Carga rápida con datos mínimos
    Dado que recibo un llamado al 911 (o un aviso interno)
    Cuando cargo fecha, código de incidente, dependencia/jurisdicción y una
      breve observación
    Entonces el caso queda creado en estado "Pendiente"
    Y se me asigna automáticamente como analista (rol Creador)

  Escenario: Carga sin número de llamado
    Dado que el pedido es un aviso interno sin número de llamado al 911
    Cuando guardo el caso sin ese dato
    Entonces el sistema lo acepta igual
```

---

## HU-01 · Actualizar estado y resultado de un caso

**Como** Analista
**Quiero** marcar un caso como Cerrado y registrar si el análisis dio
resultado Positivo, Negativo o necesita Revisión
**Para** que el histórico quede correctamente clasificado para analítica

```gherkin
Característica: Cierre de caso

  Escenario: Cierre con resultado positivo
    Dado que terminé de analizar las cámaras de un caso
    Cuando lo marco como "Cerrado" y resultado "Positivo"
    Entonces queda disponible para búsqueda y analítica con ese resultado
```

---

## HU-02 · Vincular vehículos/personas/cámaras a un caso

**Como** Analista
**Quiero** asociar al caso los vehículos, personas y cámaras analizadas
**Para** poder cruzarlos después en la ficha 360° (ver Épica 02)

```gherkin
Característica: Vinculación de entidades al caso

  Escenario: Vincular vehículo existente
    Dado que el vehículo con dominio "IAK796" ya existe en el catálogo
    Cuando lo vinculo al caso
    Entonces el caso queda relacionado a esa ficha de vehículo

  Escenario: Vehículo sin identificar aún
    Dado que el caso menciona un vehículo sin dominio confirmado
    Cuando cargo solo su descripción libre (marca/color aproximado)
    Entonces el sistema lo guarda como texto libre sin forzarme a crear
      una ficha de catálogo todavía
```

---

## HU-03 · Generar un Informe a partir de un Caso, a pedido de una Dependencia

**Como** Analista
**Quiero** generar un Informe Especial formal cuando una Dependencia lo pide
sobre un Caso ya trabajado
**Para** entregar el documento sin tener que recargar los datos del caso

```gherkin
Característica: Generación de Informe desde un Caso

  Escenario: Generar informe a pedido de una dependencia
    Dado que tengo un Caso ya cerrado (o en curso) y una Dependencia solicita
      documentación formal sobre él
    Cuando presiono "Generar Informe" desde el caso e indico la Dependencia
      solicitante y los datos de su Causa (carátula, pieza sumarial,
      circunscripción)
    Entonces se crea un nuevo Informe vinculado a ese Caso, pre-cargado con
      los vehículos/personas ya asociados
    Para que solo tenga que subir el PDF final y completar Evidencias

  Escenario: Más de una Dependencia solicita informe sobre el mismo caso
    Dado que un Caso ya tiene un Informe generado para la Comisaría 2°
    Cuando la Fiscalía N°3 solicita también documentación sobre el mismo hecho
    Entonces puedo generar un segundo Informe distinto, vinculado al mismo Caso

  Escenario: Caso sin ninguna solicitud de informe
    Dado que ninguna Dependencia pidió documentación formal sobre un Caso
    Entonces el Caso puede quedar cerrado sin nunca generar un Informe
    Y sigue contando en la analítica general de casos
```

> **Nota de alcance**: no se migra el histórico de Casos de Análisis del
> Excel (ver `00-vision-alcance.md`) — el equipo sigue llevándolo en
> paralelo. Los Casos en el sistema nuevo arrancan desde cero a partir de
> la puesta en producción.
