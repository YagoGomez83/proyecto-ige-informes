using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Personas.Commands.VincularVehiculo;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Personas;

public class VincularVehiculoACommandHandlerTests
{
    private static Persona CrearPersona() => new(RolPersona.Testigo, nombre: "Juan Pérez");

    private static Vehiculo CrearVehiculo() =>
        new("Ford", "Fiesta", "Gris", CertezaDominio.Confirmado, AccionARealizar.Identificar, "Comisaría 2°", TipoVehiculo.Auto);

    [Fact]
    public async Task Vincula_la_persona_al_vehiculo()
    {
        var dbContext = new TestAppDbContext();
        var persona = CrearPersona();
        var vehiculo = CrearVehiculo();
        dbContext.Personas.Add(persona);
        dbContext.Vehiculos.Add(vehiculo);
        await dbContext.SaveChangesAsync();

        var handler = new VincularVehiculoACommandHandler(dbContext);
        await handler.Handle(new VincularVehiculoACommand(persona.Id, vehiculo.Id), CancellationToken.None);

        var vinculo = Assert.Single(dbContext.PersonasVehiculo);
        Assert.Equal(persona.Id, vinculo.PersonaId);
        Assert.Equal(vehiculo.Id, vinculo.VehiculoId);
    }

    [Fact]
    public async Task Vincular_el_mismo_par_dos_veces_es_idempotente()
    {
        var dbContext = new TestAppDbContext();
        var persona = CrearPersona();
        var vehiculo = CrearVehiculo();
        dbContext.Personas.Add(persona);
        dbContext.Vehiculos.Add(vehiculo);
        await dbContext.SaveChangesAsync();

        var handler = new VincularVehiculoACommandHandler(dbContext);
        await handler.Handle(new VincularVehiculoACommand(persona.Id, vehiculo.Id), CancellationToken.None);
        await handler.Handle(new VincularVehiculoACommand(persona.Id, vehiculo.Id), CancellationToken.None);

        Assert.Single(dbContext.PersonasVehiculo);
    }

    [Fact]
    public async Task Rechaza_una_persona_inexistente()
    {
        var dbContext = new TestAppDbContext();
        var vehiculo = CrearVehiculo();
        dbContext.Vehiculos.Add(vehiculo);
        await dbContext.SaveChangesAsync();

        var handler = new VincularVehiculoACommandHandler(dbContext);

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            new VincularVehiculoACommand(Guid.NewGuid(), vehiculo.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Rechaza_un_vehiculo_inexistente()
    {
        var dbContext = new TestAppDbContext();
        var persona = CrearPersona();
        dbContext.Personas.Add(persona);
        await dbContext.SaveChangesAsync();

        var handler = new VincularVehiculoACommandHandler(dbContext);

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            new VincularVehiculoACommand(persona.Id, Guid.NewGuid()), CancellationToken.None));
    }
}
