using IGE.Informes.Domain.Entities;

namespace IGE.Informes.Application.Informes.Queries.ObtenerInformePorId;

public sealed record InformeDto(
    Guid Id,
    string IdRegistro,
    DateOnly FechaAnalisis,
    string? Relato,
    Guid CasoAnalisisId,
    Guid? CausaId,
    string? CausaCaratula,
    string? CausaNroPiezaSumarial,
    string? CausaCircunscripcionJudicial,
    Guid DependenciaDestinoId,
    string? PdfPath,
    EstadoInforme Estado);
