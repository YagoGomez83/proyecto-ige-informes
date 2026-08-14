using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Security;
using IGE.Informes.Application.Vehiculos.Commands.EliminarVehiculo;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Vehiculos.EliminarVehiculo;

/// <summary>
/// HU-21 · Borrado lógico de Informe, Caso de Análisis, Vehículo y Persona
/// (docs/epic-01-gestion-informes.md), Característica "Borrado lógico de un
/// Vehículo". El Command y el Handler todavía no existen — estos tests
/// deben fallar en rojo hasta que se implementen (TDD), ver
/// .claude/agents/gherkin-test-writer.md y docs/03-modelo-dominio.md,
/// "Borrado lógico de Informe, CasoAnalisis, Vehiculo y Persona".
/// </summary>
public class EliminarVehiculoCommandHandlerTests
{
    private static async Task<(TestAppDbContext DbContext, Vehiculo Vehiculo)> PrepararAsync()
    {
        var dbContext = new TestAppDbContext();
        var vehiculo = new Vehiculo("Volkswagen", "Gol", "Gris", CertezaDominio.Incierto, AccionARealizar.Detener, "Comisaría 2°", TipoVehiculo.Auto);
        dbContext.Vehiculos.Add(vehiculo);
        await dbContext.SaveChangesAsync();

        return (dbContext, vehiculo);
    }

    [Fact]
    public async Task EliminarVehiculo_DelCatalogo_DebeMarcarloComoEliminado()
    {
        var (dbContext, vehiculo) = await PrepararAsync();
        var handler = new EliminarVehiculoCommandHandler(dbContext);

        await handler.Handle(new EliminarVehiculoCommand(vehiculo.Id), CancellationToken.None);

        var actualizado = await dbContext.Vehiculos.FindAsync(vehiculo.Id);
        Assert.NotNull(actualizado);
        Assert.True(actualizado.Eliminado);
        Assert.NotNull(actualizado.FechaEliminacion);
    }

    [Fact]
    public async Task EliminarVehiculo_VehiculoInexistente_DebeRechazarConEntidadNoEncontrada()
    {
        var dbContext = new TestAppDbContext();
        var handler = new EliminarVehiculoCommandHandler(dbContext);

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            new EliminarVehiculoCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public void EliminarVehiculoCommand_DeclaraAutorizacion_ParaSupervisorYAdmin()
    {
        var atributo = typeof(EliminarVehiculoCommand)
            .GetCustomAttributes(typeof(AutorizarAttribute), inherit: true)
            .Cast<AutorizarAttribute>()
            .SingleOrDefault();

        Assert.NotNull(atributo);
        Assert.Equal(2, atributo.Roles.Count);
        Assert.Contains(Roles.Supervisor, atributo.Roles);
        Assert.Contains(Roles.Admin, atributo.Roles);
        Assert.DoesNotContain(Roles.Analista, atributo.Roles);
    }
}
