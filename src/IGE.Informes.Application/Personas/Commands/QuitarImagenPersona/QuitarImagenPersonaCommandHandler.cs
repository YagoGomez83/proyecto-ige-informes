using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;

namespace IGE.Informes.Application.Personas.Commands.QuitarImagenPersona;

public sealed class QuitarImagenPersonaCommandHandler(IAppDbContext dbContext, IFileStorage fileStorage)
    : IRequestHandler<QuitarImagenPersonaCommand>
{
    public async Task Handle(QuitarImagenPersonaCommand request, CancellationToken cancellationToken)
    {
        var imagen = await dbContext.PersonaImagenes.FindAsync([request.PersonaImagenId], cancellationToken)
            ?? throw new EntidadNoEncontradaException(nameof(PersonaImagen), request.PersonaImagenId);

        await fileStorage.EliminarAsync(imagen.ImagenPath, cancellationToken);

        dbContext.PersonaImagenes.Remove(imagen);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
