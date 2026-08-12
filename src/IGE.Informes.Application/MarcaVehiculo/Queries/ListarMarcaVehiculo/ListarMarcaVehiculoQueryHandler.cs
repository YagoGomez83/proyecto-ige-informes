using IGE.Informes.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.MarcaVehiculo.Queries.ListarMarcaVehiculo;

public sealed class ListarMarcaVehiculoQueryHandler(IAppDbContext dbContext)
    : IRequestHandler<ListarMarcaVehiculoQuery, IReadOnlyCollection<MarcaVehiculoDto>>
{
    public async Task<IReadOnlyCollection<MarcaVehiculoDto>> Handle(ListarMarcaVehiculoQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.MarcasVehiculo.AsNoTracking()
            .OrderBy(m => m.Nombre)
            .Select(m => new MarcaVehiculoDto(m.Id, m.Nombre))
            .ToListAsync(cancellationToken);
    }
}
