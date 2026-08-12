using IGE.Informes.Application.Common.Dtos;
using IGE.Informes.Application.Common.Security;
using MediatR;

namespace IGE.Informes.Application.Informes.Queries.ListarInformesPaginado;

/// <summary>
/// Listado general de Informes para "/informes" — hasta ahora esa ruta era
/// un placeholder "Próximamente" en el sidebar sin página real; un Informe
/// puntual solo se veía indirectamente (detalle de Caso o /busqueda).
/// </summary>
[Autorizar(Roles.Analista, Roles.Supervisor, Roles.Admin)]
public sealed record ListarInformesPaginadoQuery(
    int Pagina = 1,
    int TamanioPagina = 50,
    OrdenDireccion OrdenDireccion = OrdenDireccion.Desc,
    bool SoloSinCausa = false) : IRequest<PagedResult<InformeListadoDto>>;
