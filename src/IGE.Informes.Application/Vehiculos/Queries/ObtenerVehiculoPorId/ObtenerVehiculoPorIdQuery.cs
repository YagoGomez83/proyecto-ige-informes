using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Vehiculos.Queries.ObtenerVehiculoPorId;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record ObtenerVehiculoPorIdQuery(Guid VehiculoId) : IRequest<VehiculoDto?>;
