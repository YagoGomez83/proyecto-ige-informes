using IGE.Informes.Application.Common.Dtos;
using IGE.Informes.Application.Common.Security;
using IGE.Informes.Application.Dependencias.Queries.ListarDependencias;
using MediatR;

namespace IGE.Informes.Application.Dependencias.Queries.ListarDependenciasPaginado;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record ListarDependenciasPaginadoQuery(int Pagina = 1, int TamanioPagina = 50)
    : IRequest<PagedResult<DependenciaDto>>;
