using IGE.Informes.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.CentrosControlCamaras.Queries.ListarCentrosControlCamaras;

public sealed class ListarCentrosControlCamarasQueryHandler(IAppDbContext dbContext)
    : IRequestHandler<ListarCentrosControlCamarasQuery, IReadOnlyCollection<CentroControlCamarasDto>>
{
    public async Task<IReadOnlyCollection<CentroControlCamarasDto>> Handle(ListarCentrosControlCamarasQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.CentrosControlCamaras.AsNoTracking()
            .OrderBy(c => c.Sigla)
            .Select(c => new CentroControlCamarasDto(c.Id, c.Sigla, c.Nombre))
            .ToListAsync(cancellationToken);
    }
}
