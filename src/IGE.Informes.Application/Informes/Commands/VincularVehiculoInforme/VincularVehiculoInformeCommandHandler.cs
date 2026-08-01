using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Informes.Commands.VincularVehiculoInforme;

public sealed class VincularVehiculoInformeCommandHandler(IAppDbContext dbContext)
    : IRequestHandler<VincularVehiculoInformeCommand>
{
    public async Task Handle(VincularVehiculoInformeCommand request, CancellationToken cancellationToken)
    {
        var informe = await dbContext.Informes.FirstOrDefaultAsync(i => i.Id == request.InformeId, cancellationToken)
            ?? throw new EntidadNoEncontradaException(nameof(Informe), request.InformeId);

        if (informe.Estado == EstadoInforme.Publicado)
        {
            throw new InvalidOperationException(
                "Un Informe Publicado es inmutable — no se puede vincular un Vehículo nuevo.");
        }

        var vehiculoExiste = await dbContext.Vehiculos.AnyAsync(v => v.Id == request.VehiculoId, cancellationToken);
        if (!vehiculoExiste)
        {
            throw new EntidadNoEncontradaException(nameof(Vehiculo), request.VehiculoId);
        }

        var evidenciasDelInforme = await dbContext.Evidencias
            .Where(e => e.InformeId == request.InformeId)
            .ToListAsync(cancellationToken);

        if (evidenciasDelInforme.Any(e => e.VehiculoIds.Contains(request.VehiculoId)))
        {
            // Ya vinculado a este Informe — idempotente, no duplica.
            return;
        }

        var siguienteNumero = evidenciasDelInforme.Count == 0
            ? 1
            : evidenciasDelInforme.Max(e => e.NumeroImagen) + 1;

        // Chequeo de Alerta ANTES de persistir el vínculo nuevo, para ver el
        // estado previo a esta vinculación — liviano (Any/Count directo
        // sobre Evidencias), no reusa ObtenerHistorialInformesVehiculoQuery
        // completa (evitaría doble auditoría de lectura y DTOs de más
        // dentro de un Command de escritura).
        var otraEvidenciaConEsteVehiculo = await dbContext.Evidencias
            .Where(e => e.InformeId != request.InformeId && e.VehiculoIds.Contains(request.VehiculoId))
            .FirstOrDefaultAsync(cancellationToken);

        if (otraEvidenciaConEsteVehiculo is not null)
        {
            dbContext.Alertas.Add(Alerta.PorReincidencia(
                request.VehiculoId, personaId: null, informe.Id, otraEvidenciaConEsteVehiculo.InformeId));
        }
        else
        {
            var tuvoAlgunVinculoPrevio = await dbContext.Evidencias
                .AnyAsync(e => e.VehiculoIds.Contains(request.VehiculoId), cancellationToken);

            if (!tuvoAlgunVinculoPrevio)
            {
                dbContext.Alertas.Add(Alerta.PorCargaHuerfana(request.VehiculoId, personaId: null, informe.Id));
            }
        }

        var evidencia = new Evidencia(siguienteNumero, informe.Id);
        evidencia.VincularVehiculo(request.VehiculoId);
        dbContext.Evidencias.Add(evidencia);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // El Informe cambió (ej. se publicó) entre la lectura de su
            // Estado y este guardado — token de concurrencia optimista
            // sobre xmin, ver InformeConfiguration. No confiar en el Estado
            // leído al principio del Handler.
            throw new InvalidOperationException(
                "El Informe cambió de estado mientras se vinculaba el Vehículo — verificá su estado actual e intentá de nuevo.");
        }
    }
}
