using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Personas.Commands.RegistrarPersona;

public sealed class RegistrarPersonaCommandHandler(IAppDbContext dbContext) : IRequestHandler<RegistrarPersonaCommand, Guid>
{
    public async Task<Guid> Handle(RegistrarPersonaCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.Dni))
        {
            var yaExiste = await dbContext.Personas.AnyAsync(p => p.Dni == request.Dni, cancellationToken);
            if (yaExiste)
            {
                throw new EntidadDuplicadaException(nameof(Persona), nameof(Persona.Dni), request.Dni);
            }
        }

        var persona = new Persona(request.Rol, request.Nombre, request.Dni, request.Caracteristicas);

        dbContext.Personas.Add(persona);
        await dbContext.SaveChangesAsync(cancellationToken);

        return persona.Id;
    }
}
