using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;

namespace IGE.Informes.Application.Personas.Commands.IdentificarPersona;

public sealed class IdentificarPersonaCommandHandler(IAppDbContext dbContext) : IRequestHandler<IdentificarPersonaCommand>
{
    public async Task Handle(IdentificarPersonaCommand request, CancellationToken cancellationToken)
    {
        var persona = await dbContext.Personas.FindAsync([request.PersonaId], cancellationToken)
            ?? throw new EntidadNoEncontradaException(nameof(Persona), request.PersonaId);

        persona.Identificar(request.Nombre, request.Dni);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
