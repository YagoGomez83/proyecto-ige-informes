using IGE.Informes.Application.Common.Security;
using IGE.Informes.Application.Dependencias.Queries.ListarDependencias;
using MediatR;

namespace IGE.Informes.Application.Dependencias.Queries.ObtenerDependenciaPorId;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record ObtenerDependenciaPorIdQuery(Guid DependenciaId) : IRequest<DependenciaDto?>;
