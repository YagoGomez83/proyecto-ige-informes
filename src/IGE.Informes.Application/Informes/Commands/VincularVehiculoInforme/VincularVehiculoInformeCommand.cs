using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Informes.Commands.VincularVehiculoInforme;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record VincularVehiculoInformeCommand(Guid InformeId, Guid VehiculoId) : IRequest;
