using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Personas.Commands.VincularVehiculo;

public sealed class VincularVehiculoACommandHandler(IAppDbContext dbContext)
    : IRequestHandler<VincularVehiculoACommand>
{
    public async Task Handle(VincularVehiculoACommand request, CancellationToken cancellationToken)
    {
        var personaExiste = await dbContext.Personas.AnyAsync(p => p.Id == request.PersonaId, cancellationToken);
        if (!personaExiste)
        {
            throw new EntidadNoEncontradaException(nameof(Persona), request.PersonaId);
        }

        var vehiculoExiste = await dbContext.Vehiculos.AnyAsync(v => v.Id == request.VehiculoId, cancellationToken);
        if (!vehiculoExiste)
        {
            throw new EntidadNoEncontradaException(nameof(Vehiculo), request.VehiculoId);
        }

        var yaVinculado = await dbContext.PersonasVehiculo
            .AnyAsync(pv => pv.PersonaId == request.PersonaId && pv.VehiculoId == request.VehiculoId, cancellationToken);
        if (yaVinculado)
        {
            // Idempotente, no duplica.
            return;
        }

        dbContext.PersonasVehiculo.Add(new PersonaVehiculo(request.PersonaId, request.VehiculoId));

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
