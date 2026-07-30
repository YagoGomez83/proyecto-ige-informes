using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Barrios.Commands.CrearBarrio;

public sealed class CrearBarrioCommandHandler(IAppDbContext dbContext) : IRequestHandler<CrearBarrioCommand, Guid>
{
    public async Task<Guid> Handle(CrearBarrioCommand request, CancellationToken cancellationToken)
    {
        if (request.LocalidadId is { } localidadId)
        {
            var localidadExiste = await dbContext.Localidades.AnyAsync(l => l.Id == localidadId, cancellationToken);
            if (!localidadExiste)
            {
                throw new EntidadNoEncontradaException(nameof(Localidad), localidadId);
            }
        }

        if (request.LocalidadId is not null)
        {
            var yaExiste = await dbContext.Barrios.AnyAsync(
                b => b.Nombre == request.Nombre && b.LocalidadId == request.LocalidadId, cancellationToken);
            if (yaExiste)
            {
                throw new EntidadDuplicadaException(nameof(Barrio), nameof(Barrio.Nombre), request.Nombre);
            }
        }

        var barrio = new Barrio(request.Nombre, request.LocalidadId);

        dbContext.Barrios.Add(barrio);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new EntidadDuplicadaException(nameof(Barrio), nameof(Barrio.Nombre), request.Nombre);
        }

        return barrio.Id;
    }
}
