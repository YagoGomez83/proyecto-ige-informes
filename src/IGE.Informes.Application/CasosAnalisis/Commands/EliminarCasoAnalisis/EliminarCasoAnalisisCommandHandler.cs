using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;

namespace IGE.Informes.Application.CasosAnalisis.Commands.EliminarCasoAnalisis;

public sealed class EliminarCasoAnalisisCommandHandler(IAppDbContext dbContext) : IRequestHandler<EliminarCasoAnalisisCommand>
{
    public async Task Handle(EliminarCasoAnalisisCommand request, CancellationToken cancellationToken)
    {
        var caso = await dbContext.CasosAnalisis.FindAsync([request.CasoId], cancellationToken)
            ?? throw new EntidadNoEncontradaException(nameof(CasoAnalisis), request.CasoId);

        caso.Eliminar();

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
