namespace IGE.Informes.Domain.Entities;

public sealed class Barrio : IAuditable
{
    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public Guid? LocalidadId { get; private set; }

    private Barrio()
    {
    }

    public Barrio(string nombre, Guid? localidadId = null)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("El nombre del Barrio es obligatorio.", nameof(nombre));
        }

        Id = Guid.NewGuid();
        Nombre = nombre;
        LocalidadId = localidadId;
    }
}
