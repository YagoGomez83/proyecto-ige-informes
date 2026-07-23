using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Dependencias.Commands.AsignarBarrioADependencia;

public sealed class AsignarBarrioADependenciaCommandHandler(IAppDbContext dbContext) : IRequestHandler<AsignarBarrioADependenciaCommand>
{
    public async Task Handle(AsignarBarrioADependenciaCommand request, CancellationToken cancellationToken)
    {
        var dependencia = await dbContext.Dependencias.FindAsync([request.DependenciaId], cancellationToken)
            ?? throw new EntidadNoEncontradaException(nameof(Dependencia), request.DependenciaId);

        var barrioExiste = await dbContext.Barrios.AnyAsync(b => b.Id == request.BarrioId, cancellationToken);
        if (!barrioExiste)
        {
            throw new EntidadNoEncontradaException(nameof(Barrio), request.BarrioId);
        }

        dependencia.AsignarBarrio(request.BarrioId);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
