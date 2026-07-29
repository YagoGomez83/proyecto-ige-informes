using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Informes.Commands.EditarInforme;

public sealed class EditarInformeCommandHandler(IAppDbContext dbContext, ICurrentUserService currentUserService)
    : IRequestHandler<EditarInformeCommand>
{
    public async Task Handle(EditarInformeCommand request, CancellationToken cancellationToken)
    {
        var informe = await dbContext.Informes.FirstOrDefaultAsync(i => i.Id == request.InformeId, cancellationToken)
            ?? throw new EntidadNoEncontradaException(nameof(Informe), request.InformeId);

        if (informe.Estado == EstadoInforme.Publicado)
        {
            throw new InvalidOperationException(
                $"El Informe '{informe.IdRegistro}' ya está Publicado y es inmutable — no se pueden editar sus metadatos.");
        }

        if (request.FechaAnalisis is { } fechaAnalisis)
        {
            informe.CorregirFechaAnalisis(fechaAnalisis);
        }

        if (request.DependenciaDestinoId is { } dependenciaDestinoId)
        {
            var dependenciaExiste = await dbContext.Dependencias.AnyAsync(d => d.Id == dependenciaDestinoId, cancellationToken);
            if (!dependenciaExiste)
            {
                throw new EntidadNoEncontradaException(nameof(Dependencia), dependenciaDestinoId);
            }

            informe.AsignarDependenciaDestino(dependenciaDestinoId);
        }

        if (!string.IsNullOrWhiteSpace(request.CausaCaratula)
            && !string.IsNullOrWhiteSpace(request.CausaNroPiezaSumarial)
            && !string.IsNullOrWhiteSpace(request.CausaCircunscripcionJudicial))
        {
            var causa = new Causa(request.CausaCaratula, request.CausaNroPiezaSumarial, request.CausaCircunscripcionJudicial);
            dbContext.Causas.Add(causa);
            informe.AsignarCausa(causa.Id);
        }

        if (!string.IsNullOrWhiteSpace(request.Relato))
        {
            informe.CompletarRelato(request.Relato);
        }

        if (currentUserService.UsuarioId is null)
        {
            throw new ForbiddenAccessException("No hay un usuario autenticado.");
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
