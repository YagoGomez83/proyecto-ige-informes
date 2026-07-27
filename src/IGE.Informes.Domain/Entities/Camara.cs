namespace IGE.Informes.Domain.Entities;

public enum TipoCamara
{
    Domo,
    Lpr,
}

public sealed class Camara : IAuditable
{
    public Guid Id { get; private set; }
    public string Codigo { get; private set; } = string.Empty;
    public string? Ubicacion { get; private set; }
    public TipoCamara Tipo { get; private set; }
    public Guid? DependenciaId { get; private set; }
    public Guid? LocalidadId { get; private set; }
    public Guid? CentroControlCamarasId { get; private set; }

    private Camara()
    {
    }

    public Camara(string codigo, TipoCamara tipo, string? ubicacion = null, Guid? dependenciaId = null)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            throw new ArgumentException("El código de la Cámara es obligatorio.", nameof(codigo));
        }

        Id = Guid.NewGuid();
        Codigo = codigo;
        Tipo = tipo;
        Ubicacion = ubicacion;
        DependenciaId = dependenciaId;
    }

    public void CompletarUbicacion(string ubicacion)
    {
        if (string.IsNullOrWhiteSpace(ubicacion))
        {
            throw new ArgumentException("La ubicación no puede estar vacía.", nameof(ubicacion));
        }

        Ubicacion = ubicacion;
    }

    public void CambiarTipo(TipoCamara tipo)
    {
        Tipo = tipo;
    }

    /// <summary>
    /// Vincula la Cámara a la Dependencia en cuya jurisdicción se
    /// encuentra (ej. una Domo dentro de una Comisaría). Opcional — una
    /// LPR en ruta o en un paso limítrofe puede no tener Dependencia.
    /// </summary>
    public void AsignarDependencia(Guid dependenciaId)
    {
        if (dependenciaId == Guid.Empty)
        {
            throw new ArgumentException("La Dependencia es obligatoria.", nameof(dependenciaId));
        }

        DependenciaId = dependenciaId;
    }

    public void QuitarDependencia()
    {
        DependenciaId = null;
    }

    /// <summary>
    /// Localidad donde está físicamente instalada la Cámara. Opcional —
    /// una Cámara puede darse de alta antes de completar el relevamiento.
    /// </summary>
    public void AsignarLocalidad(Guid localidadId)
    {
        if (localidadId == Guid.Empty)
        {
            throw new ArgumentException("La Localidad es obligatoria.", nameof(localidadId));
        }

        LocalidadId = localidadId;
    }

    public void QuitarLocalidad()
    {
        LocalidadId = null;
    }

    /// <summary>
    /// Centro de Control de Cámaras que la monitorea. Opcional por el
    /// mismo motivo que Localidad.
    /// </summary>
    public void AsignarCentroControlCamaras(Guid centroControlCamarasId)
    {
        if (centroControlCamarasId == Guid.Empty)
        {
            throw new ArgumentException("El Centro de Control de Cámaras es obligatorio.", nameof(centroControlCamarasId));
        }

        CentroControlCamarasId = centroControlCamarasId;
    }

    public void QuitarCentroControlCamaras()
    {
        CentroControlCamarasId = null;
    }
}
