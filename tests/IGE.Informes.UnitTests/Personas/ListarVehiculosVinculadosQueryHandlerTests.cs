using IGE.Informes.Application.Personas.Queries.ListarVehiculosVinculados;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Personas;

public class ListarVehiculosVinculadosQueryHandlerTests
{
    [Fact]
    public async Task Lista_los_vehiculos_vinculados_a_la_persona()
    {
        var dbContext = new TestAppDbContext();
        var persona = new Persona(RolPersona.ConductorIdentificado, nombre: "Juan Pérez");
        var vehiculo = new Vehiculo("Ford", "Fiesta", "Gris", CertezaDominio.Confirmado, AccionARealizar.Identificar, "Comisaría 2°", TipoVehiculo.Auto, dominio: "ABC123");
        dbContext.Personas.Add(persona);
        dbContext.Vehiculos.Add(vehiculo);
        dbContext.PersonasVehiculo.Add(new PersonaVehiculo(persona.Id, vehiculo.Id));
        await dbContext.SaveChangesAsync();

        var handler = new ListarVehiculosVinculadosQueryHandler(dbContext, new FakeAuditLogger());

        var resultado = await handler.Handle(new ListarVehiculosVinculadosQuery(persona.Id), CancellationToken.None);

        var dto = Assert.Single(resultado);
        Assert.Equal(vehiculo.Id, dto.Id);
        Assert.Equal("ABC123", dto.Dominio);
    }

    [Fact]
    public async Task No_incluye_vehiculos_vinculados_a_otra_persona()
    {
        var dbContext = new TestAppDbContext();
        var personaBuscada = new Persona(RolPersona.ConductorIdentificado, nombre: "Juan Pérez");
        var otraPersona = new Persona(RolPersona.Testigo, nombre: "Ana Gómez");
        var vehiculo = new Vehiculo("Ford", "Fiesta", "Gris", CertezaDominio.Confirmado, AccionARealizar.Identificar, "Comisaría 2°", TipoVehiculo.Auto);
        dbContext.Personas.Add(personaBuscada);
        dbContext.Personas.Add(otraPersona);
        dbContext.Vehiculos.Add(vehiculo);
        dbContext.PersonasVehiculo.Add(new PersonaVehiculo(otraPersona.Id, vehiculo.Id));
        await dbContext.SaveChangesAsync();

        var handler = new ListarVehiculosVinculadosQueryHandler(dbContext, new FakeAuditLogger());

        var resultado = await handler.Handle(new ListarVehiculosVinculadosQuery(personaBuscada.Id), CancellationToken.None);

        Assert.Empty(resultado);
    }
}
