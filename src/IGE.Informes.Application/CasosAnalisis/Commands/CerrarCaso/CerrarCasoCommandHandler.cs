using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;

namespace IGE.Informes.Application.CasosAnalisis.Commands.CerrarCaso;

public sealed class CerrarCasoCommandHandler(IAppDbContext dbContext) : IRequestHandler<CerrarCasoCommand>
{
    public async Task Handle(CerrarCasoCommand request, CancellationToken cancellationToken)
    {
        var caso = await dbContext.CasosAnalisis.FindAsync([request.CasoId], cancellationToken)
            ?? throw new EntidadNoEncontradaException(nameof(CasoAnalisis), request.CasoId);

        if (request.Resultado == ResultadoCaso.Revision)
        {
            caso.MarcarEnRevision();
        }
        else
        {
            caso.CerrarConResultado(request.Resultado);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
