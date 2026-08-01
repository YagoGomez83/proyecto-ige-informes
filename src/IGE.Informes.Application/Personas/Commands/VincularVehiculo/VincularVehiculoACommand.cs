using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Personas.Commands.VincularVehiculo;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record VincularVehiculoACommand(Guid PersonaId, Guid VehiculoId) : IRequest;
