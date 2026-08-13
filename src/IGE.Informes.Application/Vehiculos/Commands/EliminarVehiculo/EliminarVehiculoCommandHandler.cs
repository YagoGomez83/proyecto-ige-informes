using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;

namespace IGE.Informes.Application.Vehiculos.Commands.EliminarVehiculo;

public sealed class EliminarVehiculoCommandHandler(IAppDbContext dbContext) : IRequestHandler<EliminarVehiculoCommand>
{
    public async Task Handle(EliminarVehiculoCommand request, CancellationToken cancellationToken)
    {
        var vehiculo = await dbContext.Vehiculos.FindAsync([request.VehiculoId], cancellationToken)
            ?? throw new EntidadNoEncontradaException(nameof(Vehiculo), request.VehiculoId);

        vehiculo.Eliminar();

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
