using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;

namespace IGE.Informes.Application.Alertas.Commands.MarcarAlertaAtendida;

public sealed class MarcarAlertaAtendidaCommandHandler(IAppDbContext dbContext, ICurrentUserService currentUserService)
    : IRequestHandler<MarcarAlertaAtendidaCommand>
{
    public async Task Handle(MarcarAlertaAtendidaCommand request, CancellationToken cancellationToken)
    {
        var alerta = await dbContext.Alertas.FindAsync([request.AlertaId], cancellationToken)
            ?? throw new EntidadNoEncontradaException(nameof(Alerta), request.AlertaId);

        var usuarioId = currentUserService.UsuarioId
            ?? throw new ForbiddenAccessException("No hay un usuario autenticado.");

        alerta.MarcarAtendida(usuarioId);

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
