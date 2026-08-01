using IGE.Informes.Domain.Entities;

namespace IGE.Informes.Application.Informes.Queries.ObtenerInformePorId;

public sealed record InformeDto(
    Guid Id,
    string IdRegistro,
    DateOnly FechaAnalisis,
    string? Relato,
    Guid? CasoAnalisisId,
    Guid? CausaId,
    string? CausaCaratula,
    string? CausaNroPiezaSumarial,
    string? CausaCircunscripcionJudicial,
    Guid DependenciaDestinoId,
    string? PdfPath,
    string? PdfUrl,
    EstadoInforme Estado,
    OrigenInforme Origen,
    IReadOnlyCollection<VehiculoVinculadoDto> VehiculosVinculados,
    IReadOnlyCollection<PersonaVinculadaDto> PersonasVinculadas);

public sealed record VehiculoVinculadoDto(Guid Id, string Marca, string Modelo, string? Dominio);

public sealed record PersonaVinculadaDto(Guid Id, string? Nombre, string? Dni, RolPersona Rol);
