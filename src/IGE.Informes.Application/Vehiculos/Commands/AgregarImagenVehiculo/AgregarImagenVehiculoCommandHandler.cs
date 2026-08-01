using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Vehiculos.Commands.AgregarImagenVehiculo;

public sealed class AgregarImagenVehiculoCommandHandler(
    IAppDbContext dbContext,
    ICurrentUserService currentUserService,
    IFileStorage fileStorage,
    IAntivirusScanner antivirusScanner) : IRequestHandler<AgregarImagenVehiculoCommand, Guid>
{
    public async Task<Guid> Handle(AgregarImagenVehiculoCommand request, CancellationToken cancellationToken)
    {
        var vehiculoExiste = await dbContext.Vehiculos.AnyAsync(v => v.Id == request.VehiculoId, cancellationToken);
        if (!vehiculoExiste)
        {
            throw new EntidadNoEncontradaException(nameof(Vehiculo), request.VehiculoId);
        }

        var usuarioId = currentUserService.UsuarioId
            ?? throw new ForbiddenAccessException("No hay un usuario autenticado.");

        // Mismo criterio fail-closed que ConfirmarCargaInformeCommandHandler:
        // si ClamAV no responde, la excepción se propaga sin capturar.
        var estaLimpio = await antivirusScanner.EstaLimpioAsync(request.Contenido, cancellationToken);
        if (!estaLimpio)
        {
            throw new ReglaDeNegocioVioladaException(
                "La imagen fue rechazada por el escaneo antivirus — no se subió ni se guardó ningún dato.");
        }

        using var stream = new MemoryStream(request.Contenido);
        var imagenPath = await fileStorage.SubirAsync(request.NombreArchivo, stream, request.TipoMime, cancellationToken);

        var imagen = new VehiculoImagen(request.VehiculoId, imagenPath, usuarioId);
        dbContext.VehiculoImagenes.Add(imagen);

        await dbContext.SaveChangesAsync(cancellationToken);

        return imagen.Id;
    }
}
