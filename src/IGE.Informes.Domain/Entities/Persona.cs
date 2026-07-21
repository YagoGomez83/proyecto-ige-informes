namespace IGE.Informes.Domain.Entities;

public enum RolPersona
{
    Denunciante,
    Damnificado,
    Sospechoso,
    ConductorIdentificado,
    Testigo,
}

public sealed class Persona : IAuditable
{
    public Guid Id { get; private set; }
    public string? Nombre { get; private set; }
    public string? Dni { get; private set; }
    public RolPersona Rol { get; private set; }
    public string? Caracteristicas { get; private set; }

    private Persona()
    {
    }

    public Persona(RolPersona rol, string? nombre = null, string? dni = null, string? caracteristicas = null)
    {
        if (string.IsNullOrWhiteSpace(nombre) && string.IsNullOrWhiteSpace(caracteristicas))
        {
            throw new ArgumentException(
                "Una Persona sin identificar debe tener al menos una característica descriptiva.",
                nameof(caracteristicas));
        }

        Id = Guid.NewGuid();
        Nombre = nombre;
        Dni = dni;
        Rol = rol;
        Caracteristicas = caracteristicas;
    }

    public void Identificar(string nombre, string? dni = null)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("El nombre es obligatorio para identificar a la Persona.", nameof(nombre));
        }

        Nombre = nombre;
        Dni = dni;
    }

    public void CambiarRol(RolPersona rol)
    {
        Rol = rol;
    }
}
