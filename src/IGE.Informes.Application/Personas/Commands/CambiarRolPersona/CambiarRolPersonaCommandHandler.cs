using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;

namespace IGE.Informes.Application.Personas.Commands.CambiarRolPersona;

public sealed class CambiarRolPersonaCommandHandler(IAppDbContext dbContext) : IRequestHandler<CambiarRolPersonaCommand>
{
    public async Task Handle(CambiarRolPersonaCommand request, CancellationToken cancellationToken)
    {
        var persona = await dbContext.Personas.FindAsync([request.PersonaId], cancellationToken)
            ?? throw new EntidadNoEncontradaException(nameof(Persona), request.PersonaId);

        persona.CambiarRol(request.NuevoRol);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
