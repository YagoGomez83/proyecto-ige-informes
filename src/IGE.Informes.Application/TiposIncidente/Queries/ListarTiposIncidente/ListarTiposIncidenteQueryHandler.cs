using IGE.Informes.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.TiposIncidente.Queries.ListarTiposIncidente;

public sealed class ListarTiposIncidenteQueryHandler(IAppDbContext dbContext)
    : IRequestHandler<ListarTiposIncidenteQuery, IReadOnlyCollection<TipoIncidenteDto>>
{
    public async Task<IReadOnlyCollection<TipoIncidenteDto>> Handle(ListarTiposIncidenteQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.TiposIncidente.AsNoTracking()
            .OrderBy(t => t.Codigo)
            .Select(t => new TipoIncidenteDto(t.Id, t.Codigo, t.Descripcion))
            .ToListAsync(cancellationToken);
    }
}
