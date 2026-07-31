using IGE.Informes.Application.CasosAnalisis.Queries.ListarCasos;
using IGE.Informes.Application.Common.Dtos;
using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.CasosAnalisis.Queries.ListarCasosPaginado;

/// <summary>
/// Versión paginada de ListarCasosQuery, para el listado de "/casos" —
/// se creó separada (no se modificó la original) porque ListarCasosQuery
/// también alimenta el <select> de Casos en Informes/CargarPdf.razor, que
/// necesita la lista completa sin paginar. Mismo criterio que
/// ListarCamarasPaginadoQuery/ListarDependenciasPaginadoQuery.
/// </summary>
[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record ListarCasosPaginadoQuery(int Pagina = 1, int TamanioPagina = 50)
    : IRequest<PagedResult<CasoAnalisisResumenDto>>;
