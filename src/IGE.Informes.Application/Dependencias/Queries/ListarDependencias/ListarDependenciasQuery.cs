using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Dependencias.Queries.ListarDependencias;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record ListarDependenciasQuery : IRequest<IReadOnlyCollection<DependenciaDto>>;
