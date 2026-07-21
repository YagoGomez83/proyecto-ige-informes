using IGE.Informes.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Camaras.Queries.ListarCamaras;

public sealed class ListarCamarasQueryHandler(IAppDbContext dbContext)
    : IRequestHandler<ListarCamarasQuery, IReadOnlyCollection<CamaraResumenDto>>
{
    public async Task<IReadOnlyCollection<CamaraResumenDto>> Handle(ListarCamarasQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.Camaras.AsNoTracking()
            .OrderBy(c => c.Codigo)
            .Select(c => new CamaraResumenDto(c.Id, c.Codigo, c.Tipo, c.Ubicacion == null))
            .ToListAsync(cancellationToken);
    }
}
