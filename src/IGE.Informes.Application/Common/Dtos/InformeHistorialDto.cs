using IGE.Informes.Domain.Entities;

namespace IGE.Informes.Application.Common.Dtos;

/// <summary>
/// Un ítem del historial de la ficha 360° (HU-07) de un Vehículo o Persona
/// — un Informe donde la entidad aparece, con la cantidad de Evidencias que
/// la mencionan (varias Evidencias del mismo Informe no duplican el ítem).
/// </summary>
public sealed record InformeHistorialDto(
    Guid InformeId,
    string IdRegistro,
    DateOnly FechaAnalisis,
    EstadoInforme Estado,
    Guid DependenciaDestinoId,
    int CantidadEvidencias,
    IReadOnlyCollection<int> NumerosDeImagen);
