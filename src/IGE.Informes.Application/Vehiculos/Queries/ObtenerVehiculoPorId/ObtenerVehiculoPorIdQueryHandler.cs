using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Vehiculos.Queries.ObtenerVehiculoPorId;

public sealed class ObtenerVehiculoPorIdQueryHandler(IAppDbContext dbContext, IAuditLogger auditLogger)
    : IRequestHandler<ObtenerVehiculoPorIdQuery, VehiculoDto?>
{
    public async Task<VehiculoDto?> Handle(ObtenerVehiculoPorIdQuery request, CancellationToken cancellationToken)
    {
        var vehiculo = await dbContext.Vehiculos.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == request.VehiculoId, cancellationToken);

        await auditLogger.RegistrarAccesoAsync("Lectura", nameof(Vehiculo), request.VehiculoId, cancellationToken);

        if (vehiculo is null)
        {
            return null;
        }

        return new VehiculoDto(
            vehiculo.Id,
            vehiculo.Marca,
            vehiculo.Modelo,
            vehiculo.Color,
            vehiculo.Dominio,
            vehiculo.DominioCerteza,
            vehiculo.Estado,
            vehiculo.AccionARealizar,
            vehiculo.AvisarA,
            vehiculo.FechaBaja,
            vehiculo.Caracteristicas,
            vehiculo.CategoriasAlertaIds);
    }
}
