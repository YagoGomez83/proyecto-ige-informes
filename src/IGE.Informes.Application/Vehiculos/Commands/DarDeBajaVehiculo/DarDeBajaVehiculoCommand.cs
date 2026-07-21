using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Vehiculos.Commands.DarDeBajaVehiculo;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record DarDeBajaVehiculoCommand(Guid VehiculoId, DateOnly FechaBaja) : IRequest;
