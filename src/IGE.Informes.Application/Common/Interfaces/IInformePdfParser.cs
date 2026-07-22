namespace IGE.Informes.Application.Common.Interfaces;

public sealed record VehiculoExtraidoDto(string Marca, string Modelo, string? Dominio, string DominioOriginal);

public sealed record PersonaExtraidaDto(string Dni, string? RolSugerido);

public sealed record EvidenciaExtraidaDto(
    int NumeroImagen,
    string? CodigoCamara,
    string? Ubicacion,
    DateTime? FechaHoraCaptura,
    string? Descripcion);

public sealed record InformeExtraidoDto(
    string? IdRegistro,
    DateOnly? FechaAnalisis,
    string? CausaCaratula,
    string? Destino,
    string? PiezaSumarial,
    string? Relato,
    IReadOnlyCollection<VehiculoExtraidoDto> Vehiculos,
    IReadOnlyCollection<PersonaExtraidaDto> Personas,
    IReadOnlyCollection<EvidenciaExtraidaDto> Evidencias)
{
    public bool RequiereRevisionManual => IdRegistro is null;
}

/// <summary>
/// Puerto hacia el parser de PDF (ver ADR-004) — Application no puede
/// depender de PdfPig/Infrastructure directamente.
/// </summary>
public interface IInformePdfParser
{
    InformeExtraidoDto Parsear(Stream pdfStream);
}
