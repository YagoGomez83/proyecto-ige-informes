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
            // Matching por N° de Pieza Sumarial exacto (no la carátula, que
            // es texto libre con variaciones de transcripción entre PDFs
            // del mismo expediente) — evita crear una Causa duplicada
            // cuando dos Informes distintos citan el mismo expediente
            // judicial real (ver docs/03-modelo-dominio.md, "Decisiones ya
            // resueltas"). Sin match exacto, se crea una Causa nueva; el
            // usuario puede elegir una sugerencia por similaridad de
            // carátula antes de llegar a este punto (ver SugerirCausasQuery).
            var causaExistente = await dbContext.Causas
                .FirstOrDefaultAsync(c => c.NroPiezaSumarial == request.CausaNroPiezaSumarial, cancellationToken);

            if (causaExistente is not null)
            {
                informe.AsignarCausa(causaExistente.Id);
            }
            else
            {
                var causa = new Causa(request.CausaCaratula, request.CausaNroPiezaSumarial, request.CausaCircunscripcionJudicial);
                dbContext.Causas.Add(causa);
                informe.AsignarCausa(causa.Id);
            }
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
