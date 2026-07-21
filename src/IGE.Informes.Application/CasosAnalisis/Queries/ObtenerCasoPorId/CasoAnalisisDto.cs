using IGE.Informes.Domain.Entities;

namespace IGE.Informes.Application.CasosAnalisis.Queries.ObtenerCasoPorId;

public sealed record CasoAnalisisDto(
    Guid Id,
    DateOnly Fecha,
    EstadoCaso Estado,
    ResultadoCaso? Resultado,
    string? NroLlamado911,
    Guid DependenciaId,
    Guid TipoIncidenteId,
    string? VehiculoInvolucradoTexto,
    string? ElementoSustraido,
    string? CamarasAnalizadasTexto,
    string? Observaciones);
