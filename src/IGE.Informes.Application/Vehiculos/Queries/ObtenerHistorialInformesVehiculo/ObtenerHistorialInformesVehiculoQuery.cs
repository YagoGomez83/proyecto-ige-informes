using IGE.Informes.Application.Common.Dtos;
using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Vehiculos.Queries.ObtenerHistorialInformesVehiculo;

/// <summary>
/// Ficha 360° de un Vehículo (HU-07, Épica 02) — todos los Informes donde
/// aparece, vía las Evidencias que lo vinculan, ordenados cronológicamente.
/// </summary>
[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record ObtenerHistorialInformesVehiculoQuery(Guid VehiculoId)
    : IRequest<IReadOnlyCollection<InformeHistorialDto>>;
