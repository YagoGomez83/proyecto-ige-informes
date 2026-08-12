using IGE.Informes.Domain.Entities;

namespace IGE.Informes.Application.Informes.Queries.ListarInformesPaginado;

public sealed record InformeListadoDto(
    Guid Id,
    string IdRegistro,
    DateOnly FechaAnalisis,
    string DependenciaNombre,
    string? CausaCaratula,
    EstadoInforme Estado);
