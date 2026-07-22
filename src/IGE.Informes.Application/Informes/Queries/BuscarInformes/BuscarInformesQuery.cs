using IGE.Informes.Application.Common.Security;
using IGE.Informes.Domain.Entities;
using MediatR;

namespace IGE.Informes.Application.Informes.Queries.BuscarInformes;

public sealed record InformeBusquedaResultDto(
    Guid Id,
    string IdRegistro,
    DateOnly FechaAnalisis,
    Guid DependenciaDestinoId,
    string? Relato,
    EstadoInforme Estado);

/// <summary>
/// Búsqueda combinada de Informes (HU-05, Épica 02) — todos los filtros son
/// opcionales y se combinan con AND. TextoLibre usa full-text search real de
/// PostgreSQL (tsvector/GIN, ver ADR-002), no LIKE/Contains.
/// </summary>
[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record BuscarInformesQuery(
    Guid? DependenciaId = null,
    Guid? TipoIncidenteId = null,
    string? DominioVehiculo = null,
    string? DniOPersona = null,
    string? TextoLibre = null,
    DateOnly? FechaDesde = null,
    DateOnly? FechaHasta = null) : IRequest<IReadOnlyCollection<InformeBusquedaResultDto>>;
