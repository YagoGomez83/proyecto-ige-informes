using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;

namespace IGE.Informes.Application.Personas.Commands.EliminarPersona;

public sealed class EliminarPersonaCommandHandler(IAppDbContext dbContext) : IRequestHandler<EliminarPersonaCommand>
{
    public async Task Handle(EliminarPersonaCommand request, CancellationToken cancellationToken)
    {
        var persona = await dbContext.Personas.FindAsync([request.PersonaId], cancellationToken)
            ?? throw new EntidadNoEncontradaException(nameof(Persona), request.PersonaId);

        persona.Eliminar();

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
