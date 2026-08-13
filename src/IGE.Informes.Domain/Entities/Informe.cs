namespace IGE.Informes.Domain.Entities;

public enum EstadoInforme
{
    Borrador,
    Publicado,
}

/// <summary>
/// De dónde nace el Informe. Los migrados (HU-04) no tienen un CasoAnalisis
/// real de origen porque el histórico de Casos no se migra (ver
/// docs/03-modelo-dominio.md, "Alcance de migración de datos") — la
/// invariante 1 del modelo ("Informe.caso_analisis_id NOT NULL") solo rige
/// para Informes con Origen=CargaIndividual.
/// </summary>
public enum OrigenInforme
{
    CargaIndividual,
    Migrado,
}

public sealed class Informe : IAuditable, IEliminableLogicamente
{
    private readonly List<InformeAnalista> _analistas = [];

    public Guid Id { get; private set; }
    public string IdRegistro { get; private set; } = string.Empty;
    public DateOnly FechaAnalisis { get; private set; }
    public string? Relato { get; private set; }
    public Guid? CasoAnalisisId { get; private set; }
    public Guid? CausaId { get; private set; }
    public Guid DependenciaDestinoId { get; private set; }
    public string? PdfPath { get; private set; }
    public EstadoInforme Estado { get; private set; }
    public OrigenInforme Origen { get; private set; }
    public bool Eliminado { get; private set; }
    public DateTime? FechaEliminacion { get; private set; }

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
        if (casoAnalisisId == Guid.Empty)
        {
            throw new ArgumentException("El Caso de Análisis de origen es obligatorio.", nameof(casoAnalisisId));
        }

        Inicializar(idRegistro, fechaAnalisis, casoAnalisisId, dependenciaDestinoId, analistaSolicitanteId, causaId, OrigenInforme.CargaIndividual);
    }

    /// <summary>
    /// Alta de un Informe migrado en lote desde PDFs históricos (HU-04) —
    /// sin CasoAnalisis de origen, porque el histórico de Casos no se migra.
    /// El usuario que ejecuta la migración queda como Analista Interviniente,
    /// igual que el solicitante en el alta individual.
    /// </summary>
    public static Informe CrearMigrado(
        string idRegistro,
        DateOnly fechaAnalisis,
        Guid dependenciaDestinoId,
        Guid usuarioMigradorId,
        Guid? causaId = null)
    {
        var informe = new Informe();
        informe.Inicializar(idRegistro, fechaAnalisis, null, dependenciaDestinoId, usuarioMigradorId, causaId, OrigenInforme.Migrado);
        return informe;
    }

    private void Inicializar(
        string idRegistro,
        DateOnly fechaAnalisis,
        Guid? casoAnalisisId,
        Guid dependenciaDestinoId,
        Guid analistaSolicitanteId,
        Guid? causaId,
        OrigenInforme origen)
    {
        if (string.IsNullOrWhiteSpace(idRegistro))
        {
            throw new ArgumentException("El ID Registro es obligatorio.", nameof(idRegistro));
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
        Origen = origen;

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

    public void CorregirFechaAnalisis(DateOnly fechaAnalisis)
    {
        if (Estado == EstadoInforme.Publicado)
        {
            throw new InvalidOperationException("Un Informe Publicado es inmutable — no se puede corregir la Fecha de Análisis.");
        }

        FechaAnalisis = fechaAnalisis;
    }

    public void CorregirIdRegistro(string idRegistro)
    {
        if (string.IsNullOrWhiteSpace(idRegistro))
        {
            throw new ArgumentException("El ID Registro es obligatorio.", nameof(idRegistro));
        }

        if (Estado == EstadoInforme.Publicado)
        {
            throw new InvalidOperationException("Un Informe Publicado es inmutable — no se puede corregir el ID Registro.");
        }

        IdRegistro = idRegistro;
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

    /// <summary>
    /// Borrado lógico (HU-21) — el registro sigue en la base para
    /// auditoría, pero deja de aparecer en listados/búsquedas (ver
    /// InformeConfiguration.HasQueryFilter). No confundir con eliminar
    /// físicamente ni con ningún otro estado.
    /// </summary>
    public void Eliminar()
    {
        if (Estado == EstadoInforme.Publicado)
        {
            throw new InvalidOperationException("Un Informe Publicado es inmutable — no se puede eliminar.");
        }

        Eliminado = true;
        FechaEliminacion = DateTime.UtcNow;
    }
}
