using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.Application.Informes.Commands.CrearInformeDesdeMigracionPendiente;

public sealed class CrearInformeDesdeMigracionPendienteCommandHandler(
    IAppDbContext dbContext,
    ICurrentUserService currentUserService,
    IAuditLogger auditLogger) : IRequestHandler<CrearInformeDesdeMigracionPendienteCommand, Guid>
{
    public async Task<Guid> Handle(CrearInformeDesdeMigracionPendienteCommand request, CancellationToken cancellationToken)
    {
        _ = currentUserService.UsuarioId
            ?? throw new ForbiddenAccessException("No hay un usuario autenticado.");

        var migracionPendiente = await dbContext.MigracionesPendientes
            .FirstOrDefaultAsync(m => m.Id == request.MigracionPendienteId, cancellationToken)
            ?? throw new EntidadNoEncontradaException(nameof(MigracionPendiente), request.MigracionPendienteId);

        // Puede pasar si el mismo PDF se cargó individualmente (HU-01)
        // mientras la MigracionPendiente seguía sin completar.
        var yaExiste = await dbContext.Informes.AnyAsync(i => i.IdRegistro == migracionPendiente.IdRegistro, cancellationToken);
        if (yaExiste)
        {
            throw new EntidadDuplicadaException(nameof(Informe), nameof(Informe.IdRegistro), migracionPendiente.IdRegistro);
        }

        var informe = migracionPendiente.CrearInformeMigrado(request.FechaAnalisis);

        dbContext.Informes.Add(informe);
        dbContext.MigracionesPendientes.Remove(migracionPendiente);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Dos Admins completando la misma MigracionPendiente casi
            // simultáneamente (doble click, dos pestañas). Según el timing
            // exacto de la carrera, Postgres puede rechazar con
            // DbUpdateConcurrencyException (choque de xmin, ver
            // MigracionPendienteConfiguration) o con una violación lisa del
            // índice único de Informe.IdRegistro (DbUpdateException base,
            // si ambos pasaron el chequeo AnyAsync de arriba antes de que
            // cualquiera confirmara) — se captura la clase base para cubrir
            // ambos casos con el mismo mensaje claro, en vez de dejar
            // pasar un error crudo de Postgres a la UI (hallazgo del
            // security-reviewer sobre la falta de concurrency token).
            throw new InvalidOperationException(
                "Esta migración pendiente ya fue completada por otro usuario — recargá la lista.");
        }

        await auditLogger.RegistrarAccesoAsync("CompletarMigracionPendiente", nameof(Informe), informe.Id, cancellationToken);

        return informe.Id;
    }
}
