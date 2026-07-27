using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Localidades.Commands.CrearLocalidad;

public sealed class CrearLocalidadCommandHandler(IAppDbContext dbContext) : IRequestHandler<CrearLocalidadCommand, Guid>
{
    public async Task<Guid> Handle(CrearLocalidadCommand request, CancellationToken cancellationToken)
    {
        var yaExiste = await dbContext.Localidades.AnyAsync(l => l.Nombre == request.Nombre, cancellationToken);
        if (yaExiste)
        {
            throw new EntidadDuplicadaException(nameof(Localidad), nameof(Localidad.Nombre), request.Nombre);
        }

        var localidad = new Localidad(request.Nombre);

        dbContext.Localidades.Add(localidad);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new EntidadDuplicadaException(nameof(Localidad), nameof(Localidad.Nombre), request.Nombre);
        }

        return localidad.Id;
    }
}
