using IGE.Informes.Domain.Entities;

namespace IGE.Informes.Application.Informes.Queries.ListarInformesPorCaso;

public sealed record InformeResumenDto(
    Guid Id,
    string IdRegistro,
    DateOnly FechaAnalisis,
    Guid DependenciaDestinoId,
    EstadoInforme Estado);
