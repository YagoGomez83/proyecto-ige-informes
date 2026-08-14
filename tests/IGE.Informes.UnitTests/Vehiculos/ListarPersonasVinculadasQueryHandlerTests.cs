using IGE.Informes.Application.Vehiculos.Queries.ListarPersonasVinculadas;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Vehiculos;

public class ListarPersonasVinculadasQueryHandlerTests
{
    [Fact]
    public async Task Lista_las_personas_vinculadas_al_vehiculo()
    {
        var dbContext = new TestAppDbContext();
        var vehiculo = new Vehiculo("Ford", "Fiesta", "Gris", CertezaDominio.Confirmado, AccionARealizar.Identificar, "Comisaría 2°", TipoVehiculo.Auto);
        var persona = new Persona(RolPersona.ConductorIdentificado, nombre: "Juan Pérez");
        dbContext.Vehiculos.Add(vehiculo);
        dbContext.Personas.Add(persona);
        dbContext.PersonasVehiculo.Add(new PersonaVehiculo(persona.Id, vehiculo.Id));
        await dbContext.SaveChangesAsync();

        var handler = new ListarPersonasVinculadasQueryHandler(dbContext, new FakeAuditLogger());

        var resultado = await handler.Handle(new ListarPersonasVinculadasQuery(vehiculo.Id), CancellationToken.None);

        var dto = Assert.Single(resultado);
        Assert.Equal(persona.Id, dto.Id);
        Assert.Equal("Juan Pérez", dto.Nombre);
    }

    [Fact]
    public async Task No_incluye_personas_vinculadas_a_otro_vehiculo()
    {
        var dbContext = new TestAppDbContext();
        var vehiculoBuscado = new Vehiculo("Ford", "Fiesta", "Gris", CertezaDominio.Confirmado, AccionARealizar.Identificar, "Comisaría 2°", TipoVehiculo.Auto);
        var otroVehiculo = new Vehiculo("Fiat", "Cronos", "Blanco", CertezaDominio.Confirmado, AccionARealizar.Identificar, "Comisaría 2°", TipoVehiculo.Auto);
        var persona = new Persona(RolPersona.ConductorIdentificado, nombre: "Juan Pérez");
        dbContext.Vehiculos.Add(vehiculoBuscado);
        dbContext.Vehiculos.Add(otroVehiculo);
        dbContext.Personas.Add(persona);
        dbContext.PersonasVehiculo.Add(new PersonaVehiculo(persona.Id, otroVehiculo.Id));
        await dbContext.SaveChangesAsync();

        var handler = new ListarPersonasVinculadasQueryHandler(dbContext, new FakeAuditLogger());

        var resultado = await handler.Handle(new ListarPersonasVinculadasQuery(vehiculoBuscado.Id), CancellationToken.None);

        Assert.Empty(resultado);
    }
}
