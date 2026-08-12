using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Dependencias.Commands.EliminarDependencia;

public sealed class EliminarDependenciaCommandHandler(IAppDbContext dbContext) : IRequestHandler<EliminarDependenciaCommand>
{
    public async Task Handle(EliminarDependenciaCommand request, CancellationToken cancellationToken)
    {
        var dependencia = await dbContext.Dependencias.FindAsync([request.DependenciaId], cancellationToken)
            ?? throw new EntidadNoEncontradaException(nameof(Dependencia), request.DependenciaId);

        var tieneInformes = await dbContext.Informes
            .AnyAsync(i => i.DependenciaDestinoId == request.DependenciaId, cancellationToken);
        if (tieneInformes)
        {
            throw new ReglaDeNegocioVioladaException(
                "No se puede eliminar la Dependencia porque tiene Informes asociados.");
        }

        var tieneCamaras = await dbContext.Camaras
            .AnyAsync(c => c.DependenciaId == request.DependenciaId, cancellationToken);
        if (tieneCamaras)
        {
            throw new ReglaDeNegocioVioladaException(
                "No se puede eliminar la Dependencia porque tiene Cámaras asociadas.");
        }

        var tieneCasosAnalisis = await dbContext.CasosAnalisis
            .AnyAsync(c => c.DependenciaId == request.DependenciaId, cancellationToken);
        if (tieneCasosAnalisis)
        {
            throw new ReglaDeNegocioVioladaException(
                "No se puede eliminar la Dependencia porque tiene Casos de Análisis asociados.");
        }

        var tieneMigracionesPendientes = await dbContext.MigracionesPendientes
            .AnyAsync(m => m.DependenciaDestinoId == request.DependenciaId, cancellationToken);
        if (tieneMigracionesPendientes)
        {
            throw new ReglaDeNegocioVioladaException(
                "No se puede eliminar la Dependencia porque tiene Migraciones Pendientes asociadas.");
        }

        var esUnidadRegionalDeOtra = await dbContext.Dependencias
            .AnyAsync(d => d.UnidadRegionalId == request.DependenciaId, cancellationToken);
        if (esUnidadRegionalDeOtra)
        {
            throw new ReglaDeNegocioVioladaException(
                "No se puede eliminar la Dependencia porque es la Unidad Regional de otra Dependencia.");
        }

        dbContext.Dependencias.Remove(dependencia);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
