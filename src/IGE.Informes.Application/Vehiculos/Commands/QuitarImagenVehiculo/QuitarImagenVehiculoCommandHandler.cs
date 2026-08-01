using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;

namespace IGE.Informes.Application.Vehiculos.Commands.QuitarImagenVehiculo;

public sealed class QuitarImagenVehiculoCommandHandler(IAppDbContext dbContext, IFileStorage fileStorage)
    : IRequestHandler<QuitarImagenVehiculoCommand>
{
    public async Task Handle(QuitarImagenVehiculoCommand request, CancellationToken cancellationToken)
    {
        var imagen = await dbContext.VehiculoImagenes.FindAsync([request.VehiculoImagenId], cancellationToken)
            ?? throw new EntidadNoEncontradaException(nameof(VehiculoImagen), request.VehiculoImagenId);

        await fileStorage.EliminarAsync(imagen.ImagenPath, cancellationToken);

        dbContext.VehiculoImagenes.Remove(imagen);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
