namespace IGE.Informes.Domain.Entities;

/// <summary>
/// Expediente judicial/policial de la Dependencia solicitante de un
/// Informe — no del IGE 4.0. Pertenece al Informe, no al CasoAnalisis.
/// </summary>
public sealed class Causa : IAuditable
{
    public Guid Id { get; private set; }
    public string Caratula { get; private set; } = string.Empty;
    public string NroPiezaSumarial { get; private set; } = string.Empty;
    public string? CircunscripcionJudicial { get; private set; }

    private Causa()
    {
    }

    public Causa(string caratula, string nroPiezaSumarial, string? circunscripcionJudicial)
    {
        if (string.IsNullOrWhiteSpace(caratula))
        {
            throw new ArgumentException("La carátula de la Causa es obligatoria.", nameof(caratula));
        }

        if (string.IsNullOrWhiteSpace(nroPiezaSumarial))
        {
            throw new ArgumentException("El N° de pieza sumarial es obligatorio.", nameof(nroPiezaSumarial));
        }

        Id = Guid.NewGuid();
        Caratula = caratula;
        NroPiezaSumarial = nroPiezaSumarial;
        CircunscripcionJudicial = string.IsNullOrWhiteSpace(circunscripcionJudicial) ? null : circunscripcionJudicial;
    }
}
