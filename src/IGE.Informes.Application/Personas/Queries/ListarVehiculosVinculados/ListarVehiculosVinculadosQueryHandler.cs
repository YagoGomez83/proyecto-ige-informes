using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Personas.Queries.ListarVehiculosVinculados;

public sealed class ListarVehiculosVinculadosQueryHandler(IAppDbContext dbContext, IAuditLogger auditLogger)
    : IRequestHandler<ListarVehiculosVinculadosQuery, IReadOnlyCollection<VehiculoVinculadoResumenDto>>
{
    public async Task<IReadOnlyCollection<VehiculoVinculadoResumenDto>> Handle(
        ListarVehiculosVinculadosQuery request, CancellationToken cancellationToken)
    {
        var vehiculoIds = await dbContext.PersonasVehiculo.AsNoTracking()
            .Where(pv => pv.PersonaId == request.PersonaId)
            .Select(pv => pv.VehiculoId)
            .ToListAsync(cancellationToken);

        var resultado = vehiculoIds.Count == 0
            ? []
            : await dbContext.Vehiculos.AsNoTracking()
                .Where(v => vehiculoIds.Contains(v.Id))
                .Select(v => new VehiculoVinculadoResumenDto(v.Id, v.Marca, v.Modelo, v.Dominio))
                .ToListAsync(cancellationToken);

        await auditLogger.RegistrarAccesoAsync("Lectura", nameof(Persona), request.PersonaId, cancellationToken);

        foreach (var vehiculo in resultado)
        {
            await auditLogger.RegistrarAccesoAsync("Lectura", nameof(Vehiculo), vehiculo.Id, cancellationToken);
        }

        return resultado;
    }
}
