using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Vehiculos.Queries.ListarImagenesVehiculo;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record ListarImagenesVehiculoQuery(Guid VehiculoId)
    : IRequest<IReadOnlyCollection<VehiculoImagenDto>>;

public sealed record VehiculoImagenDto(Guid Id, string UrlDescarga, DateTime FechaCarga);
