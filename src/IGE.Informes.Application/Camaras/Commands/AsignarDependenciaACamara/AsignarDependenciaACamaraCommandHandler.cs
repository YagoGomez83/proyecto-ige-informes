using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Camaras.Commands.AsignarDependenciaACamara;

public sealed class AsignarDependenciaACamaraCommandHandler(IAppDbContext dbContext) : IRequestHandler<AsignarDependenciaACamaraCommand>
{
    public async Task Handle(AsignarDependenciaACamaraCommand request, CancellationToken cancellationToken)
    {
        var camara = await dbContext.Camaras.FindAsync([request.CamaraId], cancellationToken)
            ?? throw new EntidadNoEncontradaException(nameof(Camara), request.CamaraId);

        var dependenciaExiste = await dbContext.Dependencias.AnyAsync(d => d.Id == request.DependenciaId, cancellationToken);
        if (!dependenciaExiste)
        {
            throw new EntidadNoEncontradaException(nameof(Dependencia), request.DependenciaId);
        }

        camara.AsignarDependencia(request.DependenciaId);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
