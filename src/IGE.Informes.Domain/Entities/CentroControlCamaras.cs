namespace IGE.Informes.Domain.Entities;

public sealed class CentroControlCamaras : IAuditable
{
    public Guid Id { get; private set; }
    public string Sigla { get; private set; } = string.Empty;
    public string Nombre { get; private set; } = string.Empty;

    private CentroControlCamaras()
    {
    }

    public CentroControlCamaras(string sigla, string nombre)
    {
        if (string.IsNullOrWhiteSpace(sigla))
        {
            throw new ArgumentException("La sigla del Centro de Control de Cámaras es obligatoria.", nameof(sigla));
        }

        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("El nombre del Centro de Control de Cámaras es obligatorio.", nameof(nombre));
        }

        Id = Guid.NewGuid();
        Sigla = sigla;
        Nombre = nombre;
    }
}
