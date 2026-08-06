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

        // El ID Registro final es el de la entidad si ya lo tenía (no se
        // pisa con lo que venga en el Command), o el que informa el
        // Administrador si la MigracionPendiente no tenía uno propio —
        // ver escenario Gherkin "Completar el ID Registro de una
        // Migración Pendiente".
        var idRegistroFinal = migracionPendiente.IdRegistro ?? request.IdRegistro;
        if (string.IsNullOrWhiteSpace(idRegistroFinal))
        {
            throw new ArgumentException(
                "Esta migración pendiente no tiene ID Registro — hay que informarlo antes de completarla.",
                nameof(request));
        }

        // Puede pasar si el mismo PDF se cargó individualmente (HU-01)
        // mientras la MigracionPendiente seguía sin completar, o si el ID
        // Registro que el Administrador tipeó ya pertenece a otro Informe
        // — el rechazo ocurre acá, antes de tocar Add/Remove, para que la
        // MigracionPendiente siga listada sin tocar y se pueda corregir
        // el dato e intentar de nuevo (escenario Gherkin "El ID Registro
        // ingresado ya existe").
        var yaExiste = await dbContext.Informes.AnyAsync(i => i.IdRegistro == idRegistroFinal, cancellationToken);
        if (yaExiste)
        {
            throw new EntidadDuplicadaException(nameof(Informe), nameof(Informe.IdRegistro), idRegistroFinal);
        }

        var informe = migracionPendiente.CrearInformeMigrado(request.FechaAnalisis, idRegistroFinal);

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
