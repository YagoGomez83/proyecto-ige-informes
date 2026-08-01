using IGE.Informes.Domain.Entities;

namespace IGE.Informes.UnitTests.Domain;

public class PersonaVehiculoTests
{
    [Fact]
    public void Alta_valida_asigna_id_y_fecha_de_vinculacion()
    {
        var personaId = Guid.NewGuid();
        var vehiculoId = Guid.NewGuid();

        var vinculo = new PersonaVehiculo(personaId, vehiculoId);

        Assert.NotEqual(Guid.Empty, vinculo.Id);
        Assert.Equal(personaId, vinculo.PersonaId);
        Assert.Equal(vehiculoId, vinculo.VehiculoId);
        Assert.True(vinculo.FechaVinculacion <= DateTime.UtcNow);
    }

    [Fact]
    public void Alta_rechaza_persona_vacia()
    {
        Assert.Throws<ArgumentException>(() => new PersonaVehiculo(Guid.Empty, Guid.NewGuid()));
    }

    [Fact]
    public void Alta_rechaza_vehiculo_vacio()
    {
        Assert.Throws<ArgumentException>(() => new PersonaVehiculo(Guid.NewGuid(), Guid.Empty));
    }
}
