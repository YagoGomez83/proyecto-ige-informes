using IGE.Informes.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Dependencias.Queries.ListarDependencias;

public sealed class ListarDependenciasQueryHandler(IAppDbContext dbContext)
    : IRequestHandler<ListarDependenciasQuery, IReadOnlyCollection<DependenciaDto>>
{
    public async Task<IReadOnlyCollection<DependenciaDto>> Handle(ListarDependenciasQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.Dependencias.AsNoTracking()
            .OrderBy(d => d.Nombre)
            .Select(d => new DependenciaDto(d.Id, d.Nombre, d.Tipo))
            .ToListAsync(cancellationToken);
    }
}
