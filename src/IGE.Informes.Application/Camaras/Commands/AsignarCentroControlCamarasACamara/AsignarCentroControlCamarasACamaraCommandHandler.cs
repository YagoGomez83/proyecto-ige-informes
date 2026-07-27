using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Camaras.Commands.AsignarCentroControlCamarasACamara;

public sealed class AsignarCentroControlCamarasACamaraCommandHandler(IAppDbContext dbContext)
    : IRequestHandler<AsignarCentroControlCamarasACamaraCommand>
{
    public async Task Handle(AsignarCentroControlCamarasACamaraCommand request, CancellationToken cancellationToken)
    {
        var camara = await dbContext.Camaras.FindAsync([request.CamaraId], cancellationToken)
            ?? throw new EntidadNoEncontradaException(nameof(Camara), request.CamaraId);

        var centroExiste = await dbContext.CentrosControlCamaras
            .AnyAsync(c => c.Id == request.CentroControlCamarasId, cancellationToken);
        if (!centroExiste)
        {
            throw new EntidadNoEncontradaException(nameof(CentroControlCamaras), request.CentroControlCamarasId);
        }

        camara.AsignarCentroControlCamaras(request.CentroControlCamarasId);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
