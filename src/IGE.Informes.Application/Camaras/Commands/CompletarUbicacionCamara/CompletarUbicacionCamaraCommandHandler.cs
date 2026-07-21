using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;

namespace IGE.Informes.Application.Camaras.Commands.CompletarUbicacionCamara;

public sealed class CompletarUbicacionCamaraCommandHandler(IAppDbContext dbContext) : IRequestHandler<CompletarUbicacionCamaraCommand>
{
    public async Task Handle(CompletarUbicacionCamaraCommand request, CancellationToken cancellationToken)
    {
        var camara = await dbContext.Camaras.FindAsync([request.CamaraId], cancellationToken)
            ?? throw new EntidadNoEncontradaException(nameof(Camara), request.CamaraId);

        camara.CompletarUbicacion(request.Ubicacion);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
