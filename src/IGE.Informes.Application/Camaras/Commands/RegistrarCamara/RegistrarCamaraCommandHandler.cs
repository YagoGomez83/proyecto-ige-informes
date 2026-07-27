using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Camaras.Commands.RegistrarCamara;

public sealed class RegistrarCamaraCommandHandler(IAppDbContext dbContext) : IRequestHandler<RegistrarCamaraCommand, Guid>
{
    public async Task<Guid> Handle(RegistrarCamaraCommand request, CancellationToken cancellationToken)
    {
        if (request.DependenciaId is { } dependenciaId)
        {
            var dependenciaExiste = await dbContext.Dependencias.AnyAsync(d => d.Id == dependenciaId, cancellationToken);
            if (!dependenciaExiste)
            {
                throw new EntidadNoEncontradaException(nameof(Dependencia), dependenciaId);
            }
        }

        var camara = new Camara(request.Codigo, request.Tipo, request.Ubicacion, request.DependenciaId);

        dbContext.Camaras.Add(camara);

        await dbContext.SaveChangesAsync(cancellationToken);

        return camara.Id;
    }
}
