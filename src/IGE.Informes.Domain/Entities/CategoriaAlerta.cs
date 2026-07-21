namespace IGE.Informes.Domain.Entities;

public sealed class CategoriaAlerta : IAuditable
{
    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = string.Empty;

    private CategoriaAlerta()
    {
    }

    public CategoriaAlerta(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("El nombre de la Categoría de Alerta es obligatorio.", nameof(nombre));
        }

        Id = Guid.NewGuid();
        Nombre = nombre;
    }
}
