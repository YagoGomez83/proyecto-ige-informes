using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Vehiculos.Queries.ListarPersonasVinculadas;

public sealed class ListarPersonasVinculadasQueryHandler(IAppDbContext dbContext, IAuditLogger auditLogger)
    : IRequestHandler<ListarPersonasVinculadasQuery, IReadOnlyCollection<PersonaVinculadaResumenDto>>
{
    public async Task<IReadOnlyCollection<PersonaVinculadaResumenDto>> Handle(
        ListarPersonasVinculadasQuery request, CancellationToken cancellationToken)
    {
        var personaIds = await dbContext.PersonasVehiculo.AsNoTracking()
            .Where(pv => pv.VehiculoId == request.VehiculoId)
            .Select(pv => pv.PersonaId)
            .ToListAsync(cancellationToken);

        var resultado = personaIds.Count == 0
            ? []
            : await dbContext.Personas.AsNoTracking()
                .Where(p => personaIds.Contains(p.Id))
                .Select(p => new PersonaVinculadaResumenDto(p.Id, p.Nombre, p.Dni, p.Rol))
                .ToListAsync(cancellationToken);

        await auditLogger.RegistrarAccesoAsync("Lectura", nameof(Vehiculo), request.VehiculoId, cancellationToken);

        foreach (var persona in resultado)
        {
            await auditLogger.RegistrarAccesoAsync("Lectura", nameof(Persona), persona.Id, cancellationToken);
        }

        return resultado;
    }
}
