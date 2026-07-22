namespace IGE.Informes.Domain.Entities;

public sealed class Evidencia : IAuditable
{
    private readonly List<Guid> _vehiculoIds = [];
    private readonly List<Guid> _personaIds = [];

    public Guid Id { get; private set; }
    public int NumeroImagen { get; private set; }
    public Guid InformeId { get; private set; }
    public Guid? CamaraId { get; private set; }
    public DateTime? FechaHoraCaptura { get; private set; }
    public string? Descripcion { get; private set; }
    public string? ImagenPath { get; private set; }

    public IReadOnlyCollection<Guid> VehiculoIds => _vehiculoIds;
    public IReadOnlyCollection<Guid> PersonaIds => _personaIds;

    private Evidencia()
    {
    }

    public Evidencia(
        int numeroImagen,
        Guid informeId,
        Guid? camaraId = null,
        DateTime? fechaHoraCaptura = null,
        string? descripcion = null,
        string? imagenPath = null)
    {
        if (numeroImagen <= 0)
        {
            throw new ArgumentException("El número de imagen debe ser positivo.", nameof(numeroImagen));
        }

        if (informeId == Guid.Empty)
        {
            throw new ArgumentException("El Informe de la Evidencia es obligatorio.", nameof(informeId));
        }

        Id = Guid.NewGuid();
        NumeroImagen = numeroImagen;
        InformeId = informeId;
        CamaraId = camaraId;
        FechaHoraCaptura = fechaHoraCaptura;
        Descripcion = descripcion;
        ImagenPath = imagenPath;
    }

    public void AsignarCamara(Guid camaraId)
    {
        if (camaraId == Guid.Empty)
        {
            throw new ArgumentException("La cámara es obligatoria.", nameof(camaraId));
        }

        CamaraId = camaraId;
    }

    public void VincularVehiculo(Guid vehiculoId)
    {
        if (vehiculoId == Guid.Empty)
        {
            throw new ArgumentException("El vehículo es obligatorio.", nameof(vehiculoId));
        }

        if (!_vehiculoIds.Contains(vehiculoId))
        {
            _vehiculoIds.Add(vehiculoId);
        }
    }

    public void VincularPersona(Guid personaId)
    {
        if (personaId == Guid.Empty)
        {
            throw new ArgumentException("La persona es obligatoria.", nameof(personaId));
        }

        if (!_personaIds.Contains(personaId))
        {
            _personaIds.Add(personaId);
        }
    }
}
