using IGE.Informes.Application.Common.Dtos;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Informes.Queries.ListarInformesPaginado;

public sealed class ListarInformesPaginadoQueryHandler(IAppDbContext dbContext, IAuditLogger auditLogger)
    : IRequestHandler<ListarInformesPaginadoQuery, PagedResult<InformeListadoDto>>
{
    public async Task<PagedResult<InformeListadoDto>> Handle(ListarInformesPaginadoQuery request, CancellationToken cancellationToken)
    {
        var query =
            from informe in dbContext.Informes.AsNoTracking()
            join dependencia in dbContext.Dependencias.AsNoTracking()
                on informe.DependenciaDestinoId equals dependencia.Id into dependenciasDelInforme
            from dependencia in dependenciasDelInforme.DefaultIfEmpty()
            join causa in dbContext.Causas.AsNoTracking()
                on informe.CausaId equals causa.Id into causasDelInforme
            from causa in causasDelInforme.DefaultIfEmpty()
            select new { informe, dependencia, causa };

        if (request.SoloSinCausa)
        {
            query = query.Where(x => x.informe.CausaId == null);
        }

        query = request.OrdenDireccion == OrdenDireccion.Asc
            ? query.OrderBy(x => x.informe.FechaAnalisis)
            : query.OrderByDescending(x => x.informe.FechaAnalisis);

        var totalItems = await query.CountAsync(cancellationToken);

        var informes = await query
            .Skip((request.Pagina - 1) * request.TamanioPagina)
            .Take(request.TamanioPagina)
            .Select(x => new InformeListadoDto(
                x.informe.Id,
                x.informe.IdRegistro,
                x.informe.FechaAnalisis,
                x.dependencia != null ? x.dependencia.Nombre : "Dependencia no encontrada",
                x.causa != null ? x.causa.Caratula : null,
                x.informe.Estado))
            .ToListAsync(cancellationToken);

        await auditLogger.RegistrarAccesoAsync("Listado", nameof(Informe), null, cancellationToken);

        return new PagedResult<InformeListadoDto>(informes, request.Pagina, request.TamanioPagina, totalItems);
    }
}
