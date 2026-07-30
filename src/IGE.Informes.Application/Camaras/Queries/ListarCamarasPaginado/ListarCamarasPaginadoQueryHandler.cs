using IGE.Informes.Application.Camaras.Queries.ListarCamaras;
using IGE.Informes.Application.Common.Dtos;
using IGE.Informes.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Camaras.Queries.ListarCamarasPaginado;

public sealed class ListarCamarasPaginadoQueryHandler(IAppDbContext dbContext)
    : IRequestHandler<ListarCamarasPaginadoQuery, PagedResult<CamaraResumenDto>>
{
    public async Task<PagedResult<CamaraResumenDto>> Handle(ListarCamarasPaginadoQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Camaras.AsNoTracking()
            .OrderBy(c => c.Codigo);

        var totalItems = await query.CountAsync(cancellationToken);

        var camaras = await query
            .Skip((request.Pagina - 1) * request.TamanioPagina)
            .Take(request.TamanioPagina)
            .Select(c => new CamaraResumenDto(c.Id, c.Codigo, c.Tipo, c.Ubicacion == null))
            .ToListAsync(cancellationToken);

        return new PagedResult<CamaraResumenDto>(camaras, request.Pagina, request.TamanioPagina, totalItems);
    }
}
