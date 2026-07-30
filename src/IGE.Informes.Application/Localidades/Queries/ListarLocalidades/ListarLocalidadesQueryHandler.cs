using IGE.Informes.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Localidades.Queries.ListarLocalidades;

public sealed class ListarLocalidadesQueryHandler(IAppDbContext dbContext)
    : IRequestHandler<ListarLocalidadesQuery, IReadOnlyCollection<LocalidadDto>>
{
    public async Task<IReadOnlyCollection<LocalidadDto>> Handle(ListarLocalidadesQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.Localidades.AsNoTracking()
            .OrderBy(l => l.Nombre)
            .Select(l => new LocalidadDto(
                l.Id,
                l.Nombre,
                dbContext.Barrios.Count(b => b.LocalidadId == l.Id)))
            .ToListAsync(cancellationToken);
    }
}
