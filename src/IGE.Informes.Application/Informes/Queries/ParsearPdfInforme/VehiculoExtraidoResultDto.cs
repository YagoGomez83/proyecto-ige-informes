namespace IGE.Informes.Application.Informes.Queries.ParsearPdfInforme;

public sealed record VehiculoExtraidoResultDto(string Marca, string Modelo, string? Dominio, string DominioOriginal);

public sealed record PersonaExtraidaResultDto(string Dni, string? RolSugerido);

public sealed record EvidenciaExtraidaResultDto(
    int NumeroImagen,
    string? CodigoCamara,
    Guid? CamaraId,
    string? Ubicacion,
    DateTime? FechaHoraCaptura,
    string? Descripcion);

public sealed record ParsearPdfInformeResultDto(
    string? IdRegistro,
    DateOnly? FechaAnalisis,
    string? CausaCaratula,
    string? Destino,
    string? PiezaSumarial,
    string? Relato,
    IReadOnlyCollection<VehiculoExtraidoResultDto> Vehiculos,
    IReadOnlyCollection<PersonaExtraidaResultDto> Personas,
    IReadOnlyCollection<EvidenciaExtraidaResultDto> Evidencias,
    bool RequiereRevisionManual,
    bool YaExisteInformeConEseIdRegistro);
