using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Alertas.Commands.MarcarAlertaAtendida;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record MarcarAlertaAtendidaCommand(Guid AlertaId) : IRequest;
