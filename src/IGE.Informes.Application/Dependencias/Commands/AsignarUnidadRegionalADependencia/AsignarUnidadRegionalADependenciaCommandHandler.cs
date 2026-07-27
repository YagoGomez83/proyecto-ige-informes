using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;

namespace IGE.Informes.Application.Dependencias.Commands.AsignarUnidadRegionalADependencia;

public sealed class AsignarUnidadRegionalADependenciaCommandHandler(IAppDbContext dbContext)
    : IRequestHandler<AsignarUnidadRegionalADependenciaCommand>
{
    public async Task Handle(AsignarUnidadRegionalADependenciaCommand request, CancellationToken cancellationToken)
    {
        var dependencia = await dbContext.Dependencias.FindAsync([request.DependenciaId], cancellationToken)
            ?? throw new EntidadNoEncontradaException(nameof(Dependencia), request.DependenciaId);

        var unidadRegional = await dbContext.Dependencias.FindAsync([request.UnidadRegionalId], cancellationToken)
            ?? throw new EntidadNoEncontradaException(nameof(Dependencia), request.UnidadRegionalId);

        if (unidadRegional.Tipo != TipoDependencia.UnidadRegional)
        {
            throw new ReglaDeNegocioVioladaException(
                $"Solo se puede asignar como Unidad Regional una Dependencia de tipo '{TipoDependencia.UnidadRegional}'.");
        }

        dependencia.AsignarUnidadRegional(request.UnidadRegionalId);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
