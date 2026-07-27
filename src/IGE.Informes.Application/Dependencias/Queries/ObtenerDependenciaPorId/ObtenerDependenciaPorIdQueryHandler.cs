using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Application.Dependencias.Queries.ListarDependencias;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Dependencias.Queries.ObtenerDependenciaPorId;

public sealed class ObtenerDependenciaPorIdQueryHandler(IAppDbContext dbContext)
    : IRequestHandler<ObtenerDependenciaPorIdQuery, DependenciaDto?>
{
    public async Task<DependenciaDto?> Handle(ObtenerDependenciaPorIdQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.Dependencias.AsNoTracking()
            .Where(d => d.Id == request.DependenciaId)
            .Select(d => new DependenciaDto(d.Id, d.Nombre, d.Tipo, d.BarrioIds, d.UnidadRegionalId))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
