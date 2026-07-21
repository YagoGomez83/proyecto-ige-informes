namespace IGE.Informes.Domain.Entities;

public enum RolInformeAnalista
{
    Interviniente,
    Firmante,
}

/// <summary>
/// Relación N:M entre Informe y el Usuario (Analista) que participa, con
/// su rol dentro de ese Informe puntual.
/// </summary>
public sealed class InformeAnalista
{
    public Guid InformeId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public RolInformeAnalista Rol { get; private set; }

    private InformeAnalista()
    {
    }

    internal InformeAnalista(Guid informeId, Guid usuarioId, RolInformeAnalista rol)
    {
        InformeId = informeId;
        UsuarioId = usuarioId;
        Rol = rol;
    }
}
