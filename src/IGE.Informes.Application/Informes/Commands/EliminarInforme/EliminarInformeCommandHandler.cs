using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;

namespace IGE.Informes.Application.Informes.Commands.EliminarInforme;

public sealed class EliminarInformeCommandHandler(IAppDbContext dbContext) : IRequestHandler<EliminarInformeCommand>
{
    public async Task Handle(EliminarInformeCommand request, CancellationToken cancellationToken)
    {
        var informe = await dbContext.Informes.FindAsync([request.InformeId], cancellationToken)
            ?? throw new EntidadNoEncontradaException(nameof(Informe), request.InformeId);

        informe.Eliminar();

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
