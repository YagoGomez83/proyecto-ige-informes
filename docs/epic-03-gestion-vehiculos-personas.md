# Épica 03 · Gestión de Vehículos y Personas

## HU-08 · Alta/edición manual de vehículo

**Como** Analista
**Quiero** dar de alta o editar un vehículo manualmente (fuera de la carga de un informe)
**Para** mantener actualizado su estado de investigación

```gherkin
Característica: Gestión de vehículos

  Escenario: Cambiar estado de un vehículo
    Dado que un vehículo está en estado "Activo en investigación"
    Cuando el analista confirma que fue identificado
    Entonces cambia su estado a "Identificado"
    Y el cambio queda en el historial de auditoría

  Escenario: Adjuntar imágenes a un vehículo
    Dado que estoy en la ficha de un vehículo
    Cuando subo una o más fotos
    Entonces quedan asociadas a ese vehículo y visibles en su ficha
```

---

## HU-09 · Alta/edición manual de persona

**Como** Analista
**Quiero** dar de alta o editar una persona (sospechoso, conductor identificado, etc.)
**Para** mantener su ficha actualizada independientemente de los informes

```gherkin
Característica: Gestión de personas

  Escenario: Persona sin identificar
    Dado que una persona aparece en un informe sin DNI ni nombre
    Cuando la doy de alta con solo características físicas
    Entonces queda registrada como "no identificada" y puede completarse luego

  Escenario: Vincular persona a un vehículo
    Dado que confirmo que una persona es el conductor de un vehículo
    Cuando los vinculo desde la ficha del informe
    Entonces esa relación queda visible en ambas fichas (vehículo y persona)
```

---

## HU-10 · Catálogo de cámaras

**Como** Administrador
**Quiero** mantener un catálogo de cámaras/dispositivos LPR con su ubicación
**Para** que la carga y búsqueda de evidencias sea consistente entre informes

```gherkin
Característica: Catálogo de cámaras

  Escenario: Autocompletar cámara al cargar evidencia
    Dado que el extractor reconoce el código "SL 18" en el PDF
    Cuando no existe ese código en el catálogo
    Entonces el sistema lo crea automáticamente como pendiente de completar
      ubicación, y lo notifica al Administrador
```
