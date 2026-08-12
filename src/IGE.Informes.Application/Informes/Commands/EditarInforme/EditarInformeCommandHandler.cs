using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Application.Common.Services;
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

        if (!string.IsNullOrWhiteSpace(request.IdRegistro) && request.IdRegistro != informe.IdRegistro)
        {
            var yaExiste = await dbContext.Informes
                .AnyAsync(i => i.Id != informe.Id && i.IdRegistro == request.IdRegistro, cancellationToken);
            if (yaExiste)
            {
                throw new EntidadDuplicadaException(nameof(Informe), nameof(Informe.IdRegistro), request.IdRegistro);
            }

            informe.CorregirIdRegistro(request.IdRegistro);
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
            // Sin match exacto, se crea una Causa nueva; el usuario puede
            // elegir una sugerencia por similaridad de carátula antes de
            // llegar a este punto (ver SugerirCausasQuery).
            var causaId = await CausaMatcher.ObtenerOCrearIdAsync(
                dbContext, request.CausaCaratula, request.CausaNroPiezaSumarial, request.CausaCircunscripcionJudicial, cancellationToken);
            informe.AsignarCausa(causaId);
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
