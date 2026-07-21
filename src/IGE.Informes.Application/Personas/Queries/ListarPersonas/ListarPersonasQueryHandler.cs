using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Personas.Queries.ListarPersonas;

public sealed class ListarPersonasQueryHandler(IAppDbContext dbContext, IAuditLogger auditLogger)
    : IRequestHandler<ListarPersonasQuery, IReadOnlyCollection<PersonaResumenDto>>
{
    public async Task<IReadOnlyCollection<PersonaResumenDto>> Handle(ListarPersonasQuery request, CancellationToken cancellationToken)
    {
        var personas = await dbContext.Personas.AsNoTracking()
            .OrderBy(p => p.Nombre)
            .Select(p => new PersonaResumenDto(p.Id, p.Nombre, p.Rol, p.Nombre != null))
            .ToListAsync(cancellationToken);

        await auditLogger.RegistrarAccesoAsync("Listado", nameof(Persona), null, cancellationToken);

        return personas;
    }
}
