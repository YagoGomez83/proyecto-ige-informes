using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;

namespace IGE.Informes.Application.CasosAnalisis.Commands.VincularVehiculoTexto;

public sealed class VincularVehiculoTextoCommandHandler(IAppDbContext dbContext)
    : IRequestHandler<VincularVehiculoTextoCommand>
{
    public async Task Handle(VincularVehiculoTextoCommand request, CancellationToken cancellationToken)
    {
        var caso = await dbContext.CasosAnalisis.FindAsync([request.CasoId], cancellationToken)
            ?? throw new EntidadNoEncontradaException(nameof(CasoAnalisis), request.CasoId);

        caso.VincularVehiculoTexto(request.DescripcionLibre);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
