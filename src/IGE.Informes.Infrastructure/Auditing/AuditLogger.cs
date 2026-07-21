using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using IGE.Informes.Infrastructure.Persistence;

namespace IGE.Informes.Infrastructure.Auditing;

/// <summary>
/// Registra auditoría de lectura (queries) sobre entidades sensibles. Las
/// altas/modificaciones/bajas se auditan aparte, automáticamente, vía
/// AuditLogInterceptor.
/// </summary>
public sealed class AuditLogger(AppDbContext dbContext, ICurrentUserService currentUserService) : IAuditLogger
{
    public async Task RegistrarAccesoAsync(string accion, string entidad, Guid? entidadId, CancellationToken cancellationToken = default)
    {
        var usuarioId = currentUserService.UsuarioId ?? Guid.Empty;

        var auditLog = new AuditLog(
            usuarioId,
            accion,
            entidad,
            entidadId,
            DateTime.UtcNow);

        dbContext.AuditLogs.Add(auditLog);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
