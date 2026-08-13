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
        var query = dbContext.Personas.AsNoTracking();

        if (request.Identificada is not null)
        {
            query = request.Identificada.Value
                ? query.Where(p => p.Nombre != null)
                : query.Where(p => p.Nombre == null);
        }

        if (request.Rol is not null)
        {
            query = query.Where(p => p.Rol == request.Rol);
        }

        // Identificada antes que Sin identificar (más útil primero), luego Rol
        // alfabético, luego Nombre alfabético dentro de cada Rol.
        query = request.Orden switch
        {
            OrdenPersonas.Rol => query.OrderBy(p => p.Rol).ThenBy(p => p.Nombre == null ? 1 : 0).ThenBy(p => p.Nombre),
            OrdenPersonas.Nombre => query.OrderBy(p => p.Nombre == null ? 1 : 0).ThenBy(p => p.Nombre),
            _ => query.OrderBy(p => p.Nombre == null ? 1 : 0).ThenBy(p => p.Rol).ThenBy(p => p.Nombre),
        };

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
