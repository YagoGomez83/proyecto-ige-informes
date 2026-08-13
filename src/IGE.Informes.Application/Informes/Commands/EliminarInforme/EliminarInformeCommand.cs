using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Informes.Commands.EliminarInforme;

[Autorizar(Roles.Supervisor, Roles.Admin)]
public sealed record EliminarInformeCommand(Guid InformeId) : IRequest;
