namespace IGE.Informes.Domain.Entities;

/// <summary>
/// Marca las entidades que admiten borrado lógico (HU-21) — permite que
/// AuditLogInterceptor distinga un borrado lógico ("BajaLogica") de
/// cualquier otra edición ("Modificacion") sin depender de reflection
/// sobre el nombre de una propiedad.
/// </summary>
public interface IEliminableLogicamente
{
    bool Eliminado { get; }
}
