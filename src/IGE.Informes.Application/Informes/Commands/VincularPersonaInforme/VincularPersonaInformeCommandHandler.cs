using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Informes.Commands.VincularPersonaInforme;

public sealed class VincularPersonaInformeCommandHandler(IAppDbContext dbContext)
    : IRequestHandler<VincularPersonaInformeCommand>
{
    public async Task Handle(VincularPersonaInformeCommand request, CancellationToken cancellationToken)
    {
        var informe = await dbContext.Informes.FirstOrDefaultAsync(i => i.Id == request.InformeId, cancellationToken)
            ?? throw new EntidadNoEncontradaException(nameof(Informe), request.InformeId);

        if (informe.Estado == EstadoInforme.Publicado)
        {
            throw new InvalidOperationException(
                "Un Informe Publicado es inmutable — no se puede vincular una Persona nueva.");
        }

        var personaExiste = await dbContext.Personas.AnyAsync(p => p.Id == request.PersonaId, cancellationToken);
        if (!personaExiste)
        {
            throw new EntidadNoEncontradaException(nameof(Persona), request.PersonaId);
        }

        var evidenciasDelInforme = await dbContext.Evidencias
            .Where(e => e.InformeId == request.InformeId)
            .ToListAsync(cancellationToken);

        if (evidenciasDelInforme.Any(e => e.PersonaIds.Contains(request.PersonaId)))
        {
            // Ya vinculada a este Informe — idempotente, no duplica.
            return;
        }

        var siguienteNumero = evidenciasDelInforme.Count == 0
            ? 1
            : evidenciasDelInforme.Max(e => e.NumeroImagen) + 1;

        // Ver comentario equivalente en VincularVehiculoInformeCommandHandler
        // sobre el chequeo de Alerta.
        var otraEvidenciaConEstaPersona = await dbContext.Evidencias
            .Where(e => e.InformeId != request.InformeId && e.PersonaIds.Contains(request.PersonaId))
            .FirstOrDefaultAsync(cancellationToken);

        if (otraEvidenciaConEstaPersona is not null)
        {
            dbContext.Alertas.Add(Alerta.PorReincidencia(
                vehiculoId: null, request.PersonaId, informe.Id, otraEvidenciaConEstaPersona.InformeId));
        }
        else
        {
            var tuvoAlgunVinculoPrevio = await dbContext.Evidencias
                .AnyAsync(e => e.PersonaIds.Contains(request.PersonaId), cancellationToken);

            if (!tuvoAlgunVinculoPrevio)
            {
                dbContext.Alertas.Add(Alerta.PorCargaHuerfana(vehiculoId: null, request.PersonaId, informe.Id));
            }
        }

        var evidencia = new Evidencia(siguienteNumero, informe.Id);
        evidencia.VincularPersona(request.PersonaId);
        dbContext.Evidencias.Add(evidencia);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Ver comentario equivalente en VincularVehiculoInformeCommandHandler.
            throw new InvalidOperationException(
                "El Informe cambió de estado mientras se vinculaba la Persona — verificá su estado actual e intentá de nuevo.");
        }
    }
}
