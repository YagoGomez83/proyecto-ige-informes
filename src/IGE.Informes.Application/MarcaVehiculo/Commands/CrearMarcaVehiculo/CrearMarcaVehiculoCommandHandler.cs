using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.MarcaVehiculo.Commands.CrearMarcaVehiculo;

public sealed class CrearMarcaVehiculoCommandHandler(IAppDbContext dbContext) : IRequestHandler<CrearMarcaVehiculoCommand, Guid>
{
    public async Task<Guid> Handle(CrearMarcaVehiculoCommand request, CancellationToken cancellationToken)
    {
        var yaExiste = await dbContext.MarcasVehiculo.AnyAsync(m => m.Nombre == request.Nombre, cancellationToken);
        if (yaExiste)
        {
            throw new EntidadDuplicadaException(nameof(Domain.Entities.MarcaVehiculo), nameof(Domain.Entities.MarcaVehiculo.Nombre), request.Nombre);
        }

        var marca = new Domain.Entities.MarcaVehiculo(request.Nombre);

        dbContext.MarcasVehiculo.Add(marca);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new EntidadDuplicadaException(nameof(Domain.Entities.MarcaVehiculo), nameof(Domain.Entities.MarcaVehiculo.Nombre), request.Nombre);
        }

        return marca.Id;
    }
}
