namespace IGE.Informes.Domain.Entities;

public sealed class AuditLog
{
    public Guid Id { get; private set; }
    public Guid UsuarioId { get; private set; }
    public string Accion { get; private set; } = string.Empty;
    public string Entidad { get; private set; } = string.Empty;
    public Guid? EntidadId { get; private set; }
    public DateTime Timestamp { get; private set; }

    private AuditLog()
    {
    }

    public AuditLog(Guid usuarioId, string accion, string entidad, Guid? entidadId, DateTime timestamp)
    {
        Id = Guid.NewGuid();
        UsuarioId = usuarioId;
        Accion = accion;
        Entidad = entidad;
        EntidadId = entidadId;
        Timestamp = timestamp;
    }
}
