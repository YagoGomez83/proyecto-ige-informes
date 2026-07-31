using IGE.Informes.Application.CasosAnalisis.Queries.ListarCasos;
using IGE.Informes.Application.Common.Dtos;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.CasosAnalisis.Queries.ListarCasosPaginado;

public sealed class ListarCasosPaginadoQueryHandler(IAppDbContext dbContext, IAuditLogger auditLogger)
    : IRequestHandler<ListarCasosPaginadoQuery, PagedResult<CasoAnalisisResumenDto>>
{
    public async Task<PagedResult<CasoAnalisisResumenDto>> Handle(ListarCasosPaginadoQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.CasosAnalisis.AsNoTracking()
            .OrderByDescending(c => c.Fecha);

        var totalItems = await query.CountAsync(cancellationToken);

        var casos = await query
            .Skip((request.Pagina - 1) * request.TamanioPagina)
            .Take(request.TamanioPagina)
            .Select(c => new CasoAnalisisResumenDto(c.Id, c.Fecha, c.Estado, c.Resultado, c.DependenciaId, c.TipoIncidenteId))
            .ToListAsync(cancellationToken);

        await auditLogger.RegistrarAccesoAsync("Listado", nameof(CasoAnalisis), null, cancellationToken);

        return new PagedResult<CasoAnalisisResumenDto>(casos, request.Pagina, request.TamanioPagina, totalItems);
    }
}
