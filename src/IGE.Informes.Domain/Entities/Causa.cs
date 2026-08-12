namespace IGE.Informes.Domain.Entities;

/// <summary>
/// Expediente judicial/policial de la Dependencia solicitante de un
/// Informe — no del IGE 4.0. Pertenece al Informe, no al CasoAnalisis.
/// </summary>
public sealed class Causa : IAuditable
{
    public Guid Id { get; private set; }
    public string Caratula { get; private set; } = string.Empty;
    public string? NroPiezaSumarial { get; private set; }
    public string? CircunscripcionJudicial { get; private set; }

    private Causa()
    {
    }

    /// <summary>
    /// El N° de Pieza Sumarial es opcional: hay Dependencias/tipos de
    /// análisis (ej. Narcotráfico) que no aportan un número de expediente
    /// real — ver docs/03-modelo-dominio.md, "Causa.NroPiezaSumarial pasa
    /// a ser opcional". Cuando es null, CausaMatcher nunca reutiliza esta
    /// Causa para otro Informe (ver Application/Common/Services/CausaMatcher).
    /// </summary>
    public Causa(string caratula, string? nroPiezaSumarial, string? circunscripcionJudicial)
    {
        if (string.IsNullOrWhiteSpace(caratula))
        {
            throw new ArgumentException("La carátula de la Causa es obligatoria.", nameof(caratula));
        }

        Id = Guid.NewGuid();
        Caratula = caratula;
        NroPiezaSumarial = string.IsNullOrWhiteSpace(nroPiezaSumarial) ? null : nroPiezaSumarial;
        CircunscripcionJudicial = string.IsNullOrWhiteSpace(circunscripcionJudicial) ? null : circunscripcionJudicial;
    }
}
