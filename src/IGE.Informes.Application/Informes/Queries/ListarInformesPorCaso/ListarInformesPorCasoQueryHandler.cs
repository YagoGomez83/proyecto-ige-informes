using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Informes.Queries.ListarInformesPorCaso;

public sealed class ListarInformesPorCasoQueryHandler(IAppDbContext dbContext, IAuditLogger auditLogger)
    : IRequestHandler<ListarInformesPorCasoQuery, IReadOnlyCollection<InformeResumenDto>>
{
    public async Task<IReadOnlyCollection<InformeResumenDto>> Handle(ListarInformesPorCasoQuery request, CancellationToken cancellationToken)
    {
        var informes = await dbContext.Informes.AsNoTracking()
            .Where(i => i.CasoAnalisisId == request.CasoAnalisisId)
            .OrderBy(i => i.FechaAnalisis)
            .Select(i => new InformeResumenDto(i.Id, i.IdRegistro, i.FechaAnalisis, i.DependenciaDestinoId, i.Estado))
            .ToListAsync(cancellationToken);

        await auditLogger.RegistrarAccesoAsync("Listado", nameof(Informe), request.CasoAnalisisId, cancellationToken);

        return informes;
    }
}
