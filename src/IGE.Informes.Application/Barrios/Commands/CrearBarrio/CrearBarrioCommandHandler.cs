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
        var yaExiste = await dbContext.Barrios.AnyAsync(b => b.Nombre == request.Nombre, cancellationToken);
        if (yaExiste)
        {
            throw new EntidadDuplicadaException(nameof(Barrio), nameof(Barrio.Nombre), request.Nombre);
        }

        var barrio = new Barrio(request.Nombre);

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
