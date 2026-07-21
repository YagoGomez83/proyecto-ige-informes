using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;

namespace IGE.Informes.Application.Personas.Commands.RegistrarPersona;

public sealed class RegistrarPersonaCommandHandler(IAppDbContext dbContext) : IRequestHandler<RegistrarPersonaCommand, Guid>
{
    public async Task<Guid> Handle(RegistrarPersonaCommand request, CancellationToken cancellationToken)
    {
        var persona = new Persona(request.Rol, request.Nombre, request.Dni, request.Caracteristicas);

        dbContext.Personas.Add(persona);
        await dbContext.SaveChangesAsync(cancellationToken);

        return persona.Id;
    }
}
