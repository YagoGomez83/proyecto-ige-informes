using IGE.Informes.Application.Common.Dtos;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Application.Dependencias.Queries.ListarDependencias;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Dependencias.Queries.ListarDependenciasPaginado;

public sealed class ListarDependenciasPaginadoQueryHandler(IAppDbContext dbContext)
    : IRequestHandler<ListarDependenciasPaginadoQuery, PagedResult<DependenciaDto>>
{
    public async Task<PagedResult<DependenciaDto>> Handle(ListarDependenciasPaginadoQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Dependencias.AsNoTracking()
            .OrderBy(d => d.Nombre);

        var totalItems = await query.CountAsync(cancellationToken);

        var dependencias = await query
            .Skip((request.Pagina - 1) * request.TamanioPagina)
            .Take(request.TamanioPagina)
            .Select(d => new DependenciaDto(d.Id, d.Nombre, d.Tipo, d.BarrioIds, d.UnidadRegionalId))
            .ToListAsync(cancellationToken);

        return new PagedResult<DependenciaDto>(dependencias, request.Pagina, request.TamanioPagina, totalItems);
    }
}
