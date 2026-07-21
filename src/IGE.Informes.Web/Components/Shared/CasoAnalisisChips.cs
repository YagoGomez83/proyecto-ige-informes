using IGE.Informes.Domain.Entities;

namespace IGE.Informes.Web.Components.Shared;

/// <summary>
/// Mapeo oficial de CasoAnalisis.Estado/Resultado a color semántico —
/// ver skill ige-design-system, sección 3. Único lugar donde se decide
/// este mapeo; las páginas solo lo consultan.
/// </summary>
public static class CasoAnalisisChips
{
    public static (string Texto, ChipSemantica Semantica) Para(EstadoCaso estado) => estado switch
    {
        EstadoCaso.Pendiente => ("Pendiente", ChipSemantica.Warning),
        EstadoCaso.EnRevision => ("En Revisión", ChipSemantica.Warning),
        EstadoCaso.Cerrado => ("Cerrado", ChipSemantica.Safe),
        _ => throw new ArgumentOutOfRangeException(nameof(estado), estado, null),
    };

    public static (string Texto, ChipSemantica Semantica) Para(ResultadoCaso? resultado) => resultado switch
    {
        null => ("Sin resultado", ChipSemantica.Neutral),
        ResultadoCaso.Positivo => ("Positivo", ChipSemantica.Safe),
        ResultadoCaso.Negativo => ("Negativo", ChipSemantica.Neutral),
        ResultadoCaso.Revision => ("Revisión", ChipSemantica.Warning),
        _ => throw new ArgumentOutOfRangeException(nameof(resultado), resultado, null),
    };
}
