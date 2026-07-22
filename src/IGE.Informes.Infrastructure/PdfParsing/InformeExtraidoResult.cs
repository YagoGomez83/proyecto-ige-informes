namespace IGE.Informes.Infrastructure.PdfParsing;

public sealed record VehiculoExtraido(string Marca, string Modelo, string? Dominio, string DominioOriginal);

public sealed record PersonaExtraida(string Dni, string? RolSugerido);

public sealed record EvidenciaExtraida(
    int NumeroImagen,
    string? CodigoCamara,
    string? Ubicacion,
    DateTime? FechaHoraCaptura,
    string? Descripcion);

/// <summary>
/// Resultado crudo de parsear un PDF de Informe Especial (ver ADR-004 y
/// el skill pdf-informe-parser). No es la entidad de dominio — es la
/// entrada para el Command que crea/completa el Informe, dejando que la
/// UI muestre qué campos no se pudieron reconocer con certeza (HU-01,
/// escenario "Extracción incompleta o de baja confianza").
/// </summary>
public sealed record InformeExtraidoResult(
    string? IdRegistro,
    DateOnly? FechaAnalisis,
    string? CausaCaratula,
    string? Destino,
    string? PiezaSumarial,
    string? Relato,
    IReadOnlyCollection<VehiculoExtraido> Vehiculos,
    IReadOnlyCollection<PersonaExtraida> Personas,
    IReadOnlyCollection<EvidenciaExtraida> Evidencias)
{
    /// <summary>
    /// El ID Registro es el campo más crítico (clave única del Informe) —
    /// si no se reconoció, el resultado no debe publicarse automáticamente
    /// (ver ADR-004: "marcar para revisión manual").
    /// </summary>
    public bool RequiereRevisionManual => IdRegistro is null;
}
