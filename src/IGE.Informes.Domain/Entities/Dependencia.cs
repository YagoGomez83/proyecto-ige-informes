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
    private readonly List<Guid> _barrioIds = [];

    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public TipoDependencia Tipo { get; private set; }
    public Guid? UnidadRegionalId { get; private set; }

    public IReadOnlyCollection<Guid> BarrioIds => _barrioIds;

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

    /// <summary>
    /// Asigna un Barrio a la jurisdicción geográfica de esta Dependencia.
    /// No toda Dependencia tiene jurisdicción geográfica (ver
    /// docs/01-glosario-dominio.md) — no hay restricción por Tipo acá, la
    /// UI simplemente no lo exige para Fiscalía/Juzgado.
    /// </summary>
    public void AsignarBarrio(Guid barrioId)
    {
        if (barrioId == Guid.Empty)
        {
            throw new ArgumentException("El Barrio a asignar es obligatorio.", nameof(barrioId));
        }

        if (!_barrioIds.Contains(barrioId))
        {
            _barrioIds.Add(barrioId);
        }
    }

    public void QuitarBarrio(Guid barrioId)
    {
        _barrioIds.Remove(barrioId);
    }

    /// <summary>
    /// Vincula esta Dependencia (típicamente una Comisaría) a la Unidad
    /// Regional de la que depende. Que el id referenciado corresponda a
    /// una Dependencia con Tipo=UnidadRegional se valida en el Handler de
    /// Application (ver docs/03-modelo-dominio.md, invariante 14) — el
    /// Domain no tiene forma de consultar otra Dependencia.
    /// </summary>
    public void AsignarUnidadRegional(Guid unidadRegionalId)
    {
        if (unidadRegionalId == Guid.Empty)
        {
            throw new ArgumentException("La Unidad Regional a asignar es obligatoria.", nameof(unidadRegionalId));
        }

        UnidadRegionalId = unidadRegionalId;
    }

    public void QuitarUnidadRegional()
    {
        UnidadRegionalId = null;
    }
}
