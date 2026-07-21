using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace IGE.Informes.Infrastructure.Auditing;

/// <summary>
/// Registra en AuditLog toda alta/modificación/baja de una entidad que
/// implemente IAuditable, antes de que se persista el SaveChanges. Genérico
/// a propósito: nace antes de que exista ninguna entidad de negocio para que
/// la auditoría no sea una feature aparte (ver CLAUDE.md, sección Seguridad).
/// </summary>
public sealed class AuditLogInterceptor(ICurrentUserService currentUserService) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            RegistrarCambios(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            RegistrarCambios(eventData.Context);
        }

        return base.SavingChanges(eventData, result);
    }

    private void RegistrarCambios(DbContext context)
    {
        var usuarioId = currentUserService.UsuarioId ?? Guid.Empty;
        var timestamp = DateTime.UtcNow;

        var entradasAuditables = context.ChangeTracker.Entries()
            .Where(entry => entry.Entity is IAuditable
                && entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var entry in entradasAuditables)
        {
            var accion = AccionDesdeEstado(entry.State);
            var auditable = (IAuditable)entry.Entity;

            var auditLog = new AuditLog(
                usuarioId,
                accion,
                entry.Entity.GetType().Name,
                auditable.Id,
                timestamp);

            context.Add(auditLog);
        }
    }

    private static string AccionDesdeEstado(EntityState state) => state switch
    {
        EntityState.Added => "Alta",
        EntityState.Modified => "Modificacion",
        EntityState.Deleted => "Baja",
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, null),
    };
}
