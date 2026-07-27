using IGE.Informes.Domain.Entities;

namespace IGE.Informes.Application.Dependencias.Queries.ListarDependencias;

public sealed record DependenciaDto(
    Guid Id,
    string Nombre,
    TipoDependencia Tipo,
    IReadOnlyCollection<Guid> BarrioIds,
    Guid? UnidadRegionalId);
