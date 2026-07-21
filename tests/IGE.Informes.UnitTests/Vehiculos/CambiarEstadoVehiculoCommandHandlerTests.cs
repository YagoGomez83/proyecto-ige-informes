using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Vehiculos.Commands.CambiarEstadoVehiculo;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Vehiculos;

public class CambiarEstadoVehiculoCommandHandlerTests
{
    private static async Task<(TestAppDbContext DbContext, Vehiculo Vehiculo)> PrepararAsync()
    {
        var dbContext = new TestAppDbContext();
        var vehiculo = new Vehiculo("Volkswagen", "Gol", "Gris", CertezaDominio.Incierto, AccionARealizar.Detener, "Comisaría 2°");
        dbContext.Vehiculos.Add(vehiculo);
        await dbContext.SaveChangesAsync();

        return (dbContext, vehiculo);
    }

    [Fact]
    public async Task Cambia_a_Identificado()
    {
        var (dbContext, vehiculo) = await PrepararAsync();
        var handler = new CambiarEstadoVehiculoCommandHandler(dbContext);

        await handler.Handle(new CambiarEstadoVehiculoCommand(vehiculo.Id, EstadoVehiculo.Identificado), CancellationToken.None);

        var actualizado = await dbContext.Vehiculos.FindAsync(vehiculo.Id);
        Assert.Equal(EstadoVehiculo.Identificado, actualizado!.Estado);
    }

    [Fact]
    public async Task Rechaza_un_vehiculo_inexistente()
    {
        var dbContext = new TestAppDbContext();
        var handler = new CambiarEstadoVehiculoCommandHandler(dbContext);

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            new CambiarEstadoVehiculoCommand(Guid.NewGuid(), EstadoVehiculo.Identificado),
            CancellationToken.None));
    }
}
