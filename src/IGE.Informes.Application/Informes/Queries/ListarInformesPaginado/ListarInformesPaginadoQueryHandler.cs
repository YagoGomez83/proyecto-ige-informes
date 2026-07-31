using IGE.Informes.Application.Common.Dtos;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Application.Informes.Queries.ListarInformesPorCaso;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Informes.Queries.ListarInformesPaginado;

public sealed class ListarInformesPaginadoQueryHandler(IAppDbContext dbContext, IAuditLogger auditLogger)
    : IRequestHandler<ListarInformesPaginadoQuery, PagedResult<InformeResumenDto>>
{
    public async Task<PagedResult<InformeResumenDto>> Handle(ListarInformesPaginadoQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Informes.AsNoTracking()
            .OrderByDescending(i => i.FechaAnalisis);

        var totalItems = await query.CountAsync(cancellationToken);

        var informes = await query
            .Skip((request.Pagina - 1) * request.TamanioPagina)
            .Take(request.TamanioPagina)
            .Select(i => new InformeResumenDto(i.Id, i.IdRegistro, i.FechaAnalisis, i.DependenciaDestinoId, i.Estado))
            .ToListAsync(cancellationToken);

        await auditLogger.RegistrarAccesoAsync("Listado", nameof(Informe), null, cancellationToken);

        return new PagedResult<InformeResumenDto>(informes, request.Pagina, request.TamanioPagina, totalItems);
    }
}
