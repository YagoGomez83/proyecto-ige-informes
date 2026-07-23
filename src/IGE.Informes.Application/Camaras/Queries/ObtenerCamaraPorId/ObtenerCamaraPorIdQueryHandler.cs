using IGE.Informes.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Camaras.Queries.ObtenerCamaraPorId;

public sealed class ObtenerCamaraPorIdQueryHandler(IAppDbContext dbContext)
    : IRequestHandler<ObtenerCamaraPorIdQuery, CamaraDto?>
{
    public async Task<CamaraDto?> Handle(ObtenerCamaraPorIdQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.Camaras.AsNoTracking()
            .Where(c => c.Id == request.CamaraId)
            .Select(c => new CamaraDto(c.Id, c.Codigo, c.Tipo, c.Ubicacion, c.DependenciaId))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
