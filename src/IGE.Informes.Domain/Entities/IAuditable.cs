namespace IGE.Informes.Domain.Entities;

/// <summary>
/// Marca las entidades cuyos cambios (alta/baja/modificación) deben quedar
/// registrados en AuditLog automáticamente vía SaveChangesInterceptor.
/// </summary>
public interface IAuditable
{
    Guid Id { get; }
}
