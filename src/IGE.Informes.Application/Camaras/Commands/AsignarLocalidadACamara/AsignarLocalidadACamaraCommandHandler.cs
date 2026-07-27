using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Camaras.Commands.AsignarLocalidadACamara;

public sealed class AsignarLocalidadACamaraCommandHandler(IAppDbContext dbContext) : IRequestHandler<AsignarLocalidadACamaraCommand>
{
    public async Task Handle(AsignarLocalidadACamaraCommand request, CancellationToken cancellationToken)
    {
        var camara = await dbContext.Camaras.FindAsync([request.CamaraId], cancellationToken)
            ?? throw new EntidadNoEncontradaException(nameof(Camara), request.CamaraId);

        var localidadExiste = await dbContext.Localidades.AnyAsync(l => l.Id == request.LocalidadId, cancellationToken);
        if (!localidadExiste)
        {
            throw new EntidadNoEncontradaException(nameof(Localidad), request.LocalidadId);
        }

        camara.AsignarLocalidad(request.LocalidadId);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
