using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;

namespace IGE.Informes.Application.Vehiculos.Commands.CambiarEstadoVehiculo;

public sealed class CambiarEstadoVehiculoCommandHandler(IAppDbContext dbContext) : IRequestHandler<CambiarEstadoVehiculoCommand>
{
    public async Task Handle(CambiarEstadoVehiculoCommand request, CancellationToken cancellationToken)
    {
        var vehiculo = await dbContext.Vehiculos.FindAsync([request.VehiculoId], cancellationToken)
            ?? throw new EntidadNoEncontradaException(nameof(Vehiculo), request.VehiculoId);

        if (request.NuevoEstado == EstadoVehiculo.Identificado)
        {
            vehiculo.MarcarIdentificado();
        }
        else
        {
            vehiculo.MarcarVigente();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
