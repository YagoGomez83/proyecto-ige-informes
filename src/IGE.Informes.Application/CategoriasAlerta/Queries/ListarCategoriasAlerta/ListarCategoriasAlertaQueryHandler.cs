using IGE.Informes.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.CategoriasAlerta.Queries.ListarCategoriasAlerta;

public sealed class ListarCategoriasAlertaQueryHandler(IAppDbContext dbContext)
    : IRequestHandler<ListarCategoriasAlertaQuery, IReadOnlyCollection<CategoriaAlertaDto>>
{
    public async Task<IReadOnlyCollection<CategoriaAlertaDto>> Handle(ListarCategoriasAlertaQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.CategoriasAlerta.AsNoTracking()
            .OrderBy(c => c.Nombre)
            .Select(c => new CategoriaAlertaDto(c.Id, c.Nombre))
            .ToListAsync(cancellationToken);
    }
}
