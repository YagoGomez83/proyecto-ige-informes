using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.ColorVehiculo.Commands.CrearColorVehiculo;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record CrearColorVehiculoCommand(string Nombre) : IRequest<Guid>;
