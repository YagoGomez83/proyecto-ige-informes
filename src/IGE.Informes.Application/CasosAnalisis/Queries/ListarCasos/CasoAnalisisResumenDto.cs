using IGE.Informes.Domain.Entities;

namespace IGE.Informes.Application.CasosAnalisis.Queries.ListarCasos;

public sealed record CasoAnalisisResumenDto(
    Guid Id,
    DateOnly Fecha,
    EstadoCaso Estado,
    ResultadoCaso? Resultado,
    Guid DependenciaId,
    Guid TipoIncidenteId);
