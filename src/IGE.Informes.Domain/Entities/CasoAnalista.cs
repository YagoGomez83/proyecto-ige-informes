namespace IGE.Informes.Domain.Entities;

public enum RolCasoAnalista
{
    Creador,
    Operador,
}

/// <summary>
/// Relación N:M entre CasoAnalisis y el Usuario (Analista) que participa,
/// con su rol dentro de ese caso puntual.
/// </summary>
public sealed class CasoAnalista
{
    public Guid CasoId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public RolCasoAnalista Rol { get; private set; }

    private CasoAnalista()
    {
    }

    internal CasoAnalista(Guid casoId, Guid usuarioId, RolCasoAnalista rol)
    {
        CasoId = casoId;
        UsuarioId = usuarioId;
        Rol = rol;
    }
}
