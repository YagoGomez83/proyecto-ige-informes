using IGE.Informes.Application.Camaras.Queries.ListarCamaras;
using IGE.Informes.Application.Common.Dtos;
using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Camaras.Queries.ListarCamarasPaginado;

[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record ListarCamarasPaginadoQuery(int Pagina = 1, int TamanioPagina = 50)
    : IRequest<PagedResult<CamaraResumenDto>>;
