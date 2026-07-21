using IGE.Informes.Domain.Entities;

namespace IGE.Informes.Web.Components.Shared;

/// <summary>
/// Mapeo oficial de Vehiculo.Estado a color semántico — ver skill
/// ige-design-system, sección 3. Único lugar donde se decide este mapeo.
/// </summary>
public static class VehiculoChips
{
    public static (string Texto, ChipSemantica Semantica) Para(EstadoVehiculo estado) => estado switch
    {
        EstadoVehiculo.Vigente => ("Vigente", ChipSemantica.Alert),
        EstadoVehiculo.Identificado => ("Identificado", ChipSemantica.Safe),
        _ => throw new ArgumentOutOfRangeException(nameof(estado), estado, null),
    };
}
