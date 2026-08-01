using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Personas.Commands.AgregarImagenPersona;

public sealed class AgregarImagenPersonaCommandHandler(
    IAppDbContext dbContext,
    ICurrentUserService currentUserService,
    IFileStorage fileStorage,
    IAntivirusScanner antivirusScanner) : IRequestHandler<AgregarImagenPersonaCommand, Guid>
{
    public async Task<Guid> Handle(AgregarImagenPersonaCommand request, CancellationToken cancellationToken)
    {
        var personaExiste = await dbContext.Personas.AnyAsync(p => p.Id == request.PersonaId, cancellationToken);
        if (!personaExiste)
        {
            throw new EntidadNoEncontradaException(nameof(Persona), request.PersonaId);
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

        var imagen = new PersonaImagen(request.PersonaId, imagenPath, usuarioId);
        dbContext.PersonaImagenes.Add(imagen);

        await dbContext.SaveChangesAsync(cancellationToken);

        return imagen.Id;
    }
}
