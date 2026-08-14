namespace IGE.Informes.Domain.Entities;

public enum EstadoVehiculo
{
    Vigente,
    Identificado,
}

public enum AccionARealizar
{
    Detener,
    Identificar,

    /// <summary>
    /// Vehículo cargado solo como referencia (ya identificado/vinculado a
    /// un Caso, Informe o Análisis pasado, sin pedido activo) — no implica
    /// ninguna intervención si se lo vuelve a detectar. Ver
    /// docs/03-modelo-dominio.md, "AccionARealizar.SinAccion".
    /// </summary>
    SinAccion,
}

public enum CertezaDominio
{
    Confirmado,
    Parcial,
    Incierto,
}

public enum TipoVehiculo
{
    Auto,
    Moto,
    Camioneta,
    Camion,
}

public sealed class Vehiculo : IAuditable, IEliminableLogicamente
{
    private readonly List<Guid> _categoriasAlertaIds = [];

    public Guid Id { get; private set; }
    public string Marca { get; private set; } = string.Empty;
    public string Modelo { get; private set; } = string.Empty;
    public string Color { get; private set; } = string.Empty;
    public string? Dominio { get; private set; }
    public CertezaDominio DominioCerteza { get; private set; }
    public EstadoVehiculo Estado { get; private set; }
    public AccionARealizar AccionARealizar { get; private set; }
    public string AvisarA { get; private set; } = string.Empty;
    public DateOnly? FechaBaja { get; private set; }
    public string? Caracteristicas { get; private set; }
    public TipoVehiculo TipoVehiculo { get; private set; }
    public string? Cilindrada { get; private set; }
    public bool Eliminado { get; private set; }
    public DateTime? FechaEliminacion { get; private set; }

    public IReadOnlyCollection<Guid> CategoriasAlertaIds => _categoriasAlertaIds;

    private Vehiculo()
    {
    }

    public Vehiculo(
        string marca,
        string modelo,
        string color,
        CertezaDominio dominioCerteza,
        AccionARealizar accionARealizar,
        string avisarA,
        TipoVehiculo tipoVehiculo,
        string? dominio = null,
        string? caracteristicas = null,
        string? cilindrada = null)
    {
        if (string.IsNullOrWhiteSpace(marca))
        {
            throw new ArgumentException("La marca del Vehículo es obligatoria.", nameof(marca));
        }

        if (string.IsNullOrWhiteSpace(modelo))
        {
            throw new ArgumentException("El modelo del Vehículo es obligatorio.", nameof(modelo));
        }

        if (string.IsNullOrWhiteSpace(color))
        {
            throw new ArgumentException("El color del Vehículo es obligatorio.", nameof(color));
        }

        if (string.IsNullOrWhiteSpace(avisarA))
        {
            throw new ArgumentException("El dato de a quién avisar es obligatorio.", nameof(avisarA));
        }

        if (tipoVehiculo == TipoVehiculo.Moto && string.IsNullOrWhiteSpace(cilindrada))
        {
            throw new ArgumentException("La cilindrada es obligatoria para motocicletas.", nameof(cilindrada));
        }

        Id = Guid.NewGuid();
        Marca = marca;
        Modelo = modelo;
        Color = color;
        Dominio = dominio;
        DominioCerteza = dominioCerteza;
        Estado = EstadoVehiculo.Vigente;
        AccionARealizar = accionARealizar;
        AvisarA = avisarA;
        Caracteristicas = caracteristicas;
        TipoVehiculo = tipoVehiculo;
        Cilindrada = tipoVehiculo == TipoVehiculo.Moto ? cilindrada : null;
    }

    public void MarcarIdentificado()
    {
        Estado = EstadoVehiculo.Identificado;
    }

    public void MarcarVigente()
    {
        Estado = EstadoVehiculo.Vigente;
        FechaBaja = null;
    }

    public void DarDeBaja(DateOnly fecha)
    {
        FechaBaja = fecha;
    }

    public void AsignarCategoriaAlerta(Guid categoriaAlertaId)
    {
        if (categoriaAlertaId == Guid.Empty)
        {
            throw new ArgumentException("La categoría de alerta es obligatoria.", nameof(categoriaAlertaId));
        }

        if (!_categoriasAlertaIds.Contains(categoriaAlertaId))
        {
            _categoriasAlertaIds.Add(categoriaAlertaId);
        }
    }

    public void QuitarCategoriaAlerta(Guid categoriaAlertaId)
    {
        _categoriasAlertaIds.Remove(categoriaAlertaId);
    }

    /// <summary>
    /// Borrado lógico (HU-21) — distinto de DarDeBaja (que solo marca fin
    /// de vigilancia activa y no oculta el Vehículo de los listados). Sin
    /// guarda de Estado: no hay ningún estado que bloquee el borrado.
    /// </summary>
    public void Eliminar()
    {
        Eliminado = true;
        FechaEliminacion = DateTime.UtcNow;
    }
}
