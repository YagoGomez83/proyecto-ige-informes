using IGE.Informes.Application.Common.Dtos;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Personas.Queries.ListarPersonas;

public sealed class ListarPersonasQueryHandler(IAppDbContext dbContext, IAuditLogger auditLogger)
    : IRequestHandler<ListarPersonasQuery, PagedResult<PersonaResumenDto>>
{
    public async Task<PagedResult<PersonaResumenDto>> Handle(ListarPersonasQuery request, CancellationToken cancellationToken)
    {
        // Identificada antes que Sin identificar (más útil primero), luego Rol
        // alfabético, luego Nombre alfabético dentro de cada Rol.
        var query = dbContext.Personas.AsNoTracking()
            .OrderBy(p => p.Nombre == null ? 1 : 0)
            .ThenBy(p => p.Rol)
            .ThenBy(p => p.Nombre);

        var totalItems = await query.CountAsync(cancellationToken);

        var personas = await query
            .Skip((request.Pagina - 1) * request.TamanioPagina)
            .Take(request.TamanioPagina)
            .Select(p => new PersonaResumenDto(p.Id, p.Nombre, p.Rol, p.Nombre != null))
            .ToListAsync(cancellationToken);

        await auditLogger.RegistrarAccesoAsync("Listado", nameof(Persona), null, cancellationToken);

        return new PagedResult<PersonaResumenDto>(personas, request.Pagina, request.TamanioPagina, totalItems);
    }
}
