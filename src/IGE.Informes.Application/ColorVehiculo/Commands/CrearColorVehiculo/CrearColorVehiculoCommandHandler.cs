using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.ColorVehiculo.Commands.CrearColorVehiculo;

public sealed class CrearColorVehiculoCommandHandler(IAppDbContext dbContext) : IRequestHandler<CrearColorVehiculoCommand, Guid>
{
    public async Task<Guid> Handle(CrearColorVehiculoCommand request, CancellationToken cancellationToken)
    {
        var yaExiste = await dbContext.ColoresVehiculo.AnyAsync(c => c.Nombre == request.Nombre, cancellationToken);
        if (yaExiste)
        {
            throw new EntidadDuplicadaException(nameof(Domain.Entities.ColorVehiculo), nameof(Domain.Entities.ColorVehiculo.Nombre), request.Nombre);
        }

        var color = new Domain.Entities.ColorVehiculo(request.Nombre);

        dbContext.ColoresVehiculo.Add(color);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new EntidadDuplicadaException(nameof(Domain.Entities.ColorVehiculo), nameof(Domain.Entities.ColorVehiculo.Nombre), request.Nombre);
        }

        return color.Id;
    }
}
