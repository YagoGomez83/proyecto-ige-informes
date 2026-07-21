using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.CasosAnalisis.Commands.RegistrarCaso;

public sealed class RegistrarCasoCommandHandler(IAppDbContext dbContext, ICurrentUserService currentUserService)
    : IRequestHandler<RegistrarCasoCommand, Guid>
{
    public async Task<Guid> Handle(RegistrarCasoCommand request, CancellationToken cancellationToken)
    {
        var dependenciaExiste = await dbContext.Dependencias
            .AnyAsync(d => d.Id == request.DependenciaId, cancellationToken);
        if (!dependenciaExiste)
        {
            throw new EntidadNoEncontradaException(nameof(Dependencia), request.DependenciaId);
        }

        var tipoIncidenteExiste = await dbContext.TiposIncidente
            .AnyAsync(t => t.Id == request.TipoIncidenteId, cancellationToken);
        if (!tipoIncidenteExiste)
        {
            throw new EntidadNoEncontradaException(nameof(TipoIncidente), request.TipoIncidenteId);
        }

        var usuarioId = currentUserService.UsuarioId
            ?? throw new ForbiddenAccessException("No hay un usuario autenticado.");

        var caso = new CasoAnalisis(
            request.Fecha,
            request.DependenciaId,
            request.TipoIncidenteId,
            usuarioId,
            request.NroLlamado911,
            request.Observaciones);

        dbContext.CasosAnalisis.Add(caso);
        await dbContext.SaveChangesAsync(cancellationToken);

        return caso.Id;
    }
}
