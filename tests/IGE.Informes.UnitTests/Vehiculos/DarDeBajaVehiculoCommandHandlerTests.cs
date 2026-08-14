using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Vehiculos.Commands.DarDeBajaVehiculo;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Vehiculos;

public class DarDeBajaVehiculoCommandHandlerTests
{
    [Fact]
    public async Task Da_de_baja_el_vehiculo_con_la_fecha_indicada()
    {
        var dbContext = new TestAppDbContext();
        var vehiculo = new Vehiculo("Volkswagen", "Gol", "Gris", CertezaDominio.Incierto, AccionARealizar.Detener, "Comisaría 2°", TipoVehiculo.Auto);
        dbContext.Vehiculos.Add(vehiculo);
        await dbContext.SaveChangesAsync();

        var handler = new DarDeBajaVehiculoCommandHandler(dbContext);
        await handler.Handle(new DarDeBajaVehiculoCommand(vehiculo.Id, new DateOnly(2026, 7, 21)), CancellationToken.None);

        var actualizado = await dbContext.Vehiculos.FindAsync(vehiculo.Id);
        Assert.Equal(new DateOnly(2026, 7, 21), actualizado!.FechaBaja);
    }

    [Fact]
    public async Task Rechaza_un_vehiculo_inexistente()
    {
        var dbContext = new TestAppDbContext();
        var handler = new DarDeBajaVehiculoCommandHandler(dbContext);

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            new DarDeBajaVehiculoCommand(Guid.NewGuid(), new DateOnly(2026, 7, 21)), CancellationToken.None));
    }
}
