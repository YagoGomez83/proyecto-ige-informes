using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;

namespace IGE.Informes.Application.Vehiculos.Commands.DarDeBajaVehiculo;

public sealed class DarDeBajaVehiculoCommandHandler(IAppDbContext dbContext) : IRequestHandler<DarDeBajaVehiculoCommand>
{
    public async Task Handle(DarDeBajaVehiculoCommand request, CancellationToken cancellationToken)
    {
        var vehiculo = await dbContext.Vehiculos.FindAsync([request.VehiculoId], cancellationToken)
            ?? throw new EntidadNoEncontradaException(nameof(Vehiculo), request.VehiculoId);

        vehiculo.DarDeBaja(request.FechaBaja);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
