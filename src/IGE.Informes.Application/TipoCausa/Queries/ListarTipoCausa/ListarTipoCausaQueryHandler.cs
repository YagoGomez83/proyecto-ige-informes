using IGE.Informes.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.TipoCausa.Queries.ListarTipoCausa;

public sealed class ListarTipoCausaQueryHandler(IAppDbContext dbContext)
    : IRequestHandler<ListarTipoCausaQuery, IReadOnlyCollection<TipoCausaDto>>
{
    public async Task<IReadOnlyCollection<TipoCausaDto>> Handle(ListarTipoCausaQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.TiposCausa.AsNoTracking()
            .OrderBy(t => t.Nombre)
            .Select(t => new TipoCausaDto(t.Id, t.Nombre))
            .ToListAsync(cancellationToken);
    }
}
