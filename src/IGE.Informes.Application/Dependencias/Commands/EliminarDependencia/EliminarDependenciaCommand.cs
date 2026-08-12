using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Dependencias.Commands.EliminarDependencia;

[Autorizar(Roles.Admin)]
public sealed record EliminarDependenciaCommand(Guid DependenciaId) : IRequest;
