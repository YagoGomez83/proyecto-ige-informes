namespace IGE.Informes.Domain.Entities;

public enum EstadoInforme
{
    Borrador,
    Publicado,
}

public sealed class Informe : IAuditable
{
    private readonly List<InformeAnalista> _analistas = [];

    public Guid Id { get; private set; }
    public string IdRegistro { get; private set; } = string.Empty;
    public DateOnly FechaAnalisis { get; private set; }
    public string? Relato { get; private set; }
    public Guid CasoAnalisisId { get; private set; }
    public Guid? CausaId { get; private set; }
    public Guid DependenciaDestinoId { get; private set; }
    public string? PdfPath { get; private set; }
    public EstadoInforme Estado { get; private set; }

    public IReadOnlyCollection<InformeAnalista> Analistas => _analistas;

    private Informe()
    {
    }

    public Informe(
        string idRegistro,
        DateOnly fechaAnalisis,
        Guid casoAnalisisId,
        Guid dependenciaDestinoId,
        Guid analistaSolicitanteId,
        Guid? causaId = null)
    {
        if (string.IsNullOrWhiteSpace(idRegistro))
        {
            throw new ArgumentException("El ID Registro es obligatorio.", nameof(idRegistro));
        }

        if (casoAnalisisId == Guid.Empty)
        {
            throw new ArgumentException("El Caso de Análisis de origen es obligatorio.", nameof(casoAnalisisId));
        }

        if (dependenciaDestinoId == Guid.Empty)
        {
            throw new ArgumentException("La Dependencia destino es obligatoria.", nameof(dependenciaDestinoId));
        }

        if (analistaSolicitanteId == Guid.Empty)
        {
            throw new ArgumentException("El analista que genera el Informe es obligatorio.", nameof(analistaSolicitanteId));
        }

        Id = Guid.NewGuid();
        IdRegistro = idRegistro;
        FechaAnalisis = fechaAnalisis;
        CasoAnalisisId = casoAnalisisId;
        DependenciaDestinoId = dependenciaDestinoId;
        CausaId = causaId;
        Estado = EstadoInforme.Borrador;

        _analistas.Add(new InformeAnalista(Id, analistaSolicitanteId, RolInformeAnalista.Interviniente));
    }
}
