using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.MarcaVehiculo.Commands.CrearMarcaVehiculo;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record CrearMarcaVehiculoCommand(string Nombre) : IRequest<Guid>;
