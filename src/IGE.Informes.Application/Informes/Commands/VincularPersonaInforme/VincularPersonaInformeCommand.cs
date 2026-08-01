using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Informes.Commands.VincularPersonaInforme;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record VincularPersonaInformeCommand(Guid InformeId, Guid PersonaId) : IRequest;
