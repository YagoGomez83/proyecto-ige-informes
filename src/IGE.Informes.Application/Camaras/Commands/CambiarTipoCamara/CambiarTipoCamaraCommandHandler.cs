using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;

namespace IGE.Informes.Application.Camaras.Commands.CambiarTipoCamara;

public sealed class CambiarTipoCamaraCommandHandler(IAppDbContext dbContext) : IRequestHandler<CambiarTipoCamaraCommand>
{
    public async Task Handle(CambiarTipoCamaraCommand request, CancellationToken cancellationToken)
    {
        var camara = await dbContext.Camaras.FindAsync([request.CamaraId], cancellationToken)
            ?? throw new EntidadNoEncontradaException(nameof(Camara), request.CamaraId);

        camara.CambiarTipo(request.NuevoTipo);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
