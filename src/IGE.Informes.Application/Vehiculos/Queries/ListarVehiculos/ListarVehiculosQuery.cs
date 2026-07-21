using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Vehiculos.Queries.ListarVehiculos;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record ListarVehiculosQuery : IRequest<IReadOnlyCollection<VehiculoResumenDto>>;
