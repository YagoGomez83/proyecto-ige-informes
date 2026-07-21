namespace IGE.Informes.Domain.Entities;

public enum TipoDependencia
{
    Comisaria,
    Fiscalia,
    Juzgado,
    Division,
    UnidadRegional,
}

public sealed class Dependencia : IAuditable
{
    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public TipoDependencia Tipo { get; private set; }

    private Dependencia()
    {
    }

    public Dependencia(string nombre, TipoDependencia tipo)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("El nombre de la Dependencia es obligatorio.", nameof(nombre));
        }

        Id = Guid.NewGuid();
        Nombre = nombre;
        Tipo = tipo;
    }
}
