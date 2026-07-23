using IGE.Informes.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Barrios.Queries.ListarBarrios;

public sealed class ListarBarriosQueryHandler(IAppDbContext dbContext)
    : IRequestHandler<ListarBarriosQuery, IReadOnlyCollection<BarrioDto>>
{
    public async Task<IReadOnlyCollection<BarrioDto>> Handle(ListarBarriosQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.Barrios.AsNoTracking()
            .OrderBy(b => b.Nombre)
            .Select(b => new BarrioDto(b.Id, b.Nombre))
            .ToListAsync(cancellationToken);
    }
}
