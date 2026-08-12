using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.MarcaVehiculo.Commands.CrearMarcaVehiculo;

[Autorizar(Roles.Admin)]
public sealed record CrearMarcaVehiculoCommand(string Nombre) : IRequest<Guid>;
