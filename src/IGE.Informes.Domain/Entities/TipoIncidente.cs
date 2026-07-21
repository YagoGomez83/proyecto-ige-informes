namespace IGE.Informes.Domain.Entities;

public sealed class TipoIncidente : IAuditable
{
    public Guid Id { get; private set; }
    public string Codigo { get; private set; } = string.Empty;
    public string Descripcion { get; private set; } = string.Empty;

    private TipoIncidente()
    {
    }

    public TipoIncidente(string codigo, string descripcion)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            throw new ArgumentException("El código del Tipo de Incidente es obligatorio.", nameof(codigo));
        }

        if (string.IsNullOrWhiteSpace(descripcion))
        {
            throw new ArgumentException("La descripción del Tipo de Incidente es obligatoria.", nameof(descripcion));
        }

        Id = Guid.NewGuid();
        Codigo = codigo;
        Descripcion = descripcion;
    }
}
