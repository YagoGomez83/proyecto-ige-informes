namespace IGE.Informes.Domain.Entities;

public enum EstadoCaso
{
    Pendiente,
    EnRevision,
    Cerrado,
}

public enum ResultadoCaso
{
    Positivo,
    Negativo,
    Revision,
}

public sealed class CasoAnalisis : IAuditable, IEliminableLogicamente
{
    private readonly List<CasoAnalista> _analistas = [];

    public Guid Id { get; private set; }
    public DateOnly Fecha { get; private set; }
    public EstadoCaso Estado { get; private set; }
    public ResultadoCaso? Resultado { get; private set; }
    public string? NroLlamado911 { get; private set; }
    public Guid DependenciaId { get; private set; }
    public Guid TipoIncidenteId { get; private set; }
    public string? VehiculoInvolucradoTexto { get; private set; }
    public string? ElementoSustraido { get; private set; }
    public string? CamarasAnalizadasTexto { get; private set; }
    public string? Observaciones { get; private set; }
    public bool Eliminado { get; private set; }
    public DateTime? FechaEliminacion { get; private set; }

    public IReadOnlyCollection<CasoAnalista> Analistas => _analistas;

    private CasoAnalisis()
    {
    }

    public CasoAnalisis(
        DateOnly fecha,
        Guid dependenciaId,
        Guid tipoIncidenteId,
        Guid creadorUsuarioId,
        string? nroLlamado911 = null,
        string? observaciones = null)
    {
        if (dependenciaId == Guid.Empty)
        {
            throw new ArgumentException("La Dependencia es obligatoria.", nameof(dependenciaId));
        }

        if (tipoIncidenteId == Guid.Empty)
        {
            throw new ArgumentException("El Tipo de Incidente es obligatorio.", nameof(tipoIncidenteId));
        }

        if (creadorUsuarioId == Guid.Empty)
        {
            throw new ArgumentException("El usuario creador es obligatorio.", nameof(creadorUsuarioId));
        }

        Id = Guid.NewGuid();
        Fecha = fecha;
        Estado = EstadoCaso.Pendiente;
        Resultado = null;
        DependenciaId = dependenciaId;
        TipoIncidenteId = tipoIncidenteId;
        NroLlamado911 = nroLlamado911;
        Observaciones = observaciones;

        _analistas.Add(new CasoAnalista(Id, creadorUsuarioId, RolCasoAnalista.Creador));
    }

    public void CerrarConResultado(ResultadoCaso resultado)
    {
        Estado = EstadoCaso.Cerrado;
        Resultado = resultado;
    }

    public void MarcarEnRevision()
    {
        Estado = EstadoCaso.EnRevision;
        Resultado = ResultadoCaso.Revision;
    }

    public void AsignarAnalista(Guid usuarioId, RolCasoAnalista rol)
    {
        if (usuarioId == Guid.Empty)
        {
            throw new ArgumentException("El usuario a asignar es obligatorio.", nameof(usuarioId));
        }

        if (_analistas.Any(a => a.UsuarioId == usuarioId))
        {
            return;
        }

        _analistas.Add(new CasoAnalista(Id, usuarioId, rol));
    }

    public void VincularVehiculoTexto(string descripcionLibre)
    {
        if (string.IsNullOrWhiteSpace(descripcionLibre))
        {
            throw new ArgumentException("La descripción del vehículo no puede estar vacía.", nameof(descripcionLibre));
        }

        VehiculoInvolucradoTexto = descripcionLibre;
    }

    public void VincularCamaraTexto(string camaraLibre)
    {
        if (string.IsNullOrWhiteSpace(camaraLibre))
        {
            throw new ArgumentException("La referencia de cámara no puede estar vacía.", nameof(camaraLibre));
        }

        CamarasAnalizadasTexto = string.IsNullOrWhiteSpace(CamarasAnalizadasTexto)
            ? camaraLibre
            : $"{CamarasAnalizadasTexto}; {camaraLibre}";
    }

    /// <summary>
    /// Borrado lógico (HU-21) — sin guarda por Estado: a diferencia de
    /// Informe.Publicado, "Cerrado" no es una barrera de inmutabilidad
    /// hoy en CasoAnalisis (ver docs/03-modelo-dominio.md).
    /// </summary>
    public void Eliminar()
    {
        Eliminado = true;
        FechaEliminacion = DateTime.UtcNow;
    }
}
