using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.CasosAnalisis.Queries.ListarCasos;

public sealed class ListarCasosQueryHandler(IAppDbContext dbContext, IAuditLogger auditLogger)
    : IRequestHandler<ListarCasosQuery, IReadOnlyCollection<CasoAnalisisResumenDto>>
{
    public async Task<IReadOnlyCollection<CasoAnalisisResumenDto>> Handle(ListarCasosQuery request, CancellationToken cancellationToken)
    {
        var casos = await dbContext.CasosAnalisis.AsNoTracking()
            .Select(c => new CasoAnalisisResumenDto(c.Id, c.Fecha, c.Estado, c.Resultado, c.DependenciaId, c.TipoIncidenteId))
            .ToListAsync(cancellationToken);

        await auditLogger.RegistrarAccesoAsync("Listado", nameof(CasoAnalisis), null, cancellationToken);

        return casos;
    }
}
