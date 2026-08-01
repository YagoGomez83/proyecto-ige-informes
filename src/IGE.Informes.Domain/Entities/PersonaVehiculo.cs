namespace IGE.Informes.Domain.Entities;

public sealed class PersonaVehiculo : IAuditable
{
    public Guid Id { get; private set; }
    public Guid PersonaId { get; private set; }
    public Guid VehiculoId { get; private set; }
    public DateTime FechaVinculacion { get; private set; }

    private PersonaVehiculo()
    {
    }

    public PersonaVehiculo(Guid personaId, Guid vehiculoId)
    {
        if (personaId == Guid.Empty)
        {
            throw new ArgumentException("La Persona es obligatoria.", nameof(personaId));
        }

        if (vehiculoId == Guid.Empty)
        {
            throw new ArgumentException("El Vehículo es obligatorio.", nameof(vehiculoId));
        }

        Id = Guid.NewGuid();
        PersonaId = personaId;
        VehiculoId = vehiculoId;
        FechaVinculacion = DateTime.UtcNow;
    }
}
