using IGE.Informes.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.ColorVehiculo.Queries.ListarColorVehiculo;

public sealed class ListarColorVehiculoQueryHandler(IAppDbContext dbContext)
    : IRequestHandler<ListarColorVehiculoQuery, IReadOnlyCollection<ColorVehiculoDto>>
{
    public async Task<IReadOnlyCollection<ColorVehiculoDto>> Handle(ListarColorVehiculoQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.ColoresVehiculo.AsNoTracking()
            .OrderBy(c => c.Nombre)
            .Select(c => new ColorVehiculoDto(c.Id, c.Nombre))
            .ToListAsync(cancellationToken);
    }
}
