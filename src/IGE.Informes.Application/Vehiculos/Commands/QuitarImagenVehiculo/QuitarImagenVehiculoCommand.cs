using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Vehiculos.Commands.QuitarImagenVehiculo;

[Autorizar(Roles.Supervisor, Roles.Admin)]
public sealed record QuitarImagenVehiculoCommand(Guid VehiculoImagenId) : IRequest;
