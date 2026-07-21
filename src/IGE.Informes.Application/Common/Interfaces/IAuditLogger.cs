namespace IGE.Informes.Application.Common.Interfaces;

/// <summary>
/// Puerto para registrar auditoría de lectura (queries) sobre entidades
/// sensibles. Los cambios (alta/baja/modificación) se auditan aparte, de
/// forma automática, vía SaveChangesInterceptor.
/// </summary>
public interface IAuditLogger
{
    Task RegistrarAccesoAsync(string accion, string entidad, Guid? entidadId, CancellationToken cancellationToken = default);
}
