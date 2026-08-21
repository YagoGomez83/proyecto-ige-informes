using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Dependencias.Commands.AsignarBarrioADependencia;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record AsignarBarrioADependenciaCommand(Guid DependenciaId, Guid BarrioId) : IRequest;
