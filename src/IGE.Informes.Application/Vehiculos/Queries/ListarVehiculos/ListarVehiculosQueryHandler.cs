using IGE.Informes.Application.Common.Dtos;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Vehiculos.Queries.ListarVehiculos;

public sealed class ListarVehiculosQueryHandler(IAppDbContext dbContext, IAuditLogger auditLogger)
    : IRequestHandler<ListarVehiculosQuery, PagedResult<VehiculoResumenDto>>
{
    public async Task<PagedResult<VehiculoResumenDto>> Handle(ListarVehiculosQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Vehiculos.AsNoTracking();

        if (request.Estado is not null)
        {
            query = query.Where(v => v.Estado == request.Estado);
        }

        // Vigente antes que Identificado (más urgente primero) — mismo criterio
        // que el chip de estado (VehiculoChips), no alfabético del nombre del enum.
        query = request.Orden == OrdenVehiculos.Estado
            ? query.OrderBy(v => v.Estado == EstadoVehiculo.Vigente ? 0 : 1).ThenBy(v => v.Marca).ThenBy(v => v.Modelo)
            : query.OrderBy(v => v.Marca).ThenBy(v => v.Modelo);

        var totalItems = await query.CountAsync(cancellationToken);

        var vehiculos = await query
            .Skip((request.Pagina - 1) * request.TamanioPagina)
            .Take(request.TamanioPagina)
            .Select(v => new VehiculoResumenDto(v.Id, v.Marca, v.Modelo, v.Dominio, v.Estado, v.AccionARealizar, v.TipoVehiculo))
            .ToListAsync(cancellationToken);

        await auditLogger.RegistrarAccesoAsync("Listado", nameof(Vehiculo), null, cancellationToken);

        return new PagedResult<VehiculoResumenDto>(vehiculos, request.Pagina, request.TamanioPagina, totalItems);
    }
}
