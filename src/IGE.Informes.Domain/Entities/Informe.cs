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

    public void CompletarRelato(string relato)
    {
        if (Estado == EstadoInforme.Publicado)
        {
            throw new InvalidOperationException("Un Informe Publicado es inmutable — no se puede editar el relato.");
        }

        Relato = relato;
    }

    public void AsignarPdf(string pdfPath)
    {
        if (string.IsNullOrWhiteSpace(pdfPath))
        {
            throw new ArgumentException("La ruta del PDF no puede estar vacía.", nameof(pdfPath));
        }

        if (Estado == EstadoInforme.Publicado)
        {
            throw new InvalidOperationException("Un Informe Publicado es inmutable — no se puede reemplazar el PDF.");
        }

        PdfPath = pdfPath;
    }

    public void AsignarCausa(Guid causaId)
    {
        if (Estado == EstadoInforme.Publicado)
        {
            throw new InvalidOperationException("Un Informe Publicado es inmutable — no se puede modificar la Causa.");
        }

        CausaId = causaId;
    }

    public void AsignarDependenciaDestino(Guid dependenciaDestinoId)
    {
        if (dependenciaDestinoId == Guid.Empty)
        {
            throw new ArgumentException("La Dependencia destino es obligatoria.", nameof(dependenciaDestinoId));
        }

        if (Estado == EstadoInforme.Publicado)
        {
            throw new InvalidOperationException("Un Informe Publicado es inmutable — no se puede modificar la Dependencia destino.");
        }

        DependenciaDestinoId = dependenciaDestinoId;
    }

    public void AgregarFirmante(Guid usuarioId)
    {
        if (usuarioId == Guid.Empty)
        {
            throw new ArgumentException("El usuario firmante es obligatorio.", nameof(usuarioId));
        }

        var existente = _analistas.FirstOrDefault(a => a.UsuarioId == usuarioId);
        if (existente is not null)
        {
            _analistas.Remove(existente);
        }

        _analistas.Add(new InformeAnalista(Id, usuarioId, RolInformeAnalista.Firmante));
    }

    public void Publicar()
    {
        if (Estado == EstadoInforme.Publicado)
        {
            return;
        }

        if (CausaId is null)
        {
            throw new InvalidOperationException("No se puede publicar el Informe: falta la Causa.");
        }

        if (DependenciaDestinoId == Guid.Empty)
        {
            throw new InvalidOperationException("No se puede publicar el Informe: falta la Dependencia destino.");
        }

        if (!_analistas.Any(a => a.Rol == RolInformeAnalista.Firmante))
        {
            throw new InvalidOperationException("No se puede publicar el Informe: falta un Analista Firmante.");
        }

        Estado = EstadoInforme.Publicado;
    }
}
