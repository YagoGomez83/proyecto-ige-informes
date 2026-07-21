using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Personas.Queries.ObtenerPersonaPorId;

public sealed class ObtenerPersonaPorIdQueryHandler(IAppDbContext dbContext, IAuditLogger auditLogger)
    : IRequestHandler<ObtenerPersonaPorIdQuery, PersonaDto?>
{
    public async Task<PersonaDto?> Handle(ObtenerPersonaPorIdQuery request, CancellationToken cancellationToken)
    {
        var persona = await dbContext.Personas.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PersonaId, cancellationToken);

        await auditLogger.RegistrarAccesoAsync("Lectura", nameof(Persona), request.PersonaId, cancellationToken);

        if (persona is null)
        {
            return null;
        }

        return new PersonaDto(persona.Id, persona.Nombre, persona.Dni, persona.Rol, persona.Caracteristicas);
    }
}
