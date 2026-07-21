using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Vehiculos.Queries.ListarVehiculos;

public sealed class ListarVehiculosQueryHandler(IAppDbContext dbContext, IAuditLogger auditLogger)
    : IRequestHandler<ListarVehiculosQuery, IReadOnlyCollection<VehiculoResumenDto>>
{
    public async Task<IReadOnlyCollection<VehiculoResumenDto>> Handle(ListarVehiculosQuery request, CancellationToken cancellationToken)
    {
        var vehiculos = await dbContext.Vehiculos.AsNoTracking()
            .OrderBy(v => v.Marca).ThenBy(v => v.Modelo)
            .Select(v => new VehiculoResumenDto(v.Id, v.Marca, v.Modelo, v.Dominio, v.Estado, v.AccionARealizar))
            .ToListAsync(cancellationToken);

        await auditLogger.RegistrarAccesoAsync("Listado", nameof(Vehiculo), null, cancellationToken);

        return vehiculos;
    }
}
