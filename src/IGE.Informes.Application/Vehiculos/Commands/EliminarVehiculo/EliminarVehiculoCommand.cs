using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Vehiculos.Commands.EliminarVehiculo;

[Autorizar(Roles.Supervisor, Roles.Admin)]
public sealed record EliminarVehiculoCommand(Guid VehiculoId) : IRequest;
