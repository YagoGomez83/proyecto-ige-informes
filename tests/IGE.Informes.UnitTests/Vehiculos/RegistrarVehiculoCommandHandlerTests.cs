using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Vehiculos.Commands.RegistrarVehiculo;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Vehiculos;

public class RegistrarVehiculoCommandHandlerTests
{
    [Fact]
    public async Task Registra_el_vehiculo_en_estado_Vigente_con_sus_categorias_de_alerta()
    {
        var dbContext = new TestAppDbContext();
        var robado = new CategoriaAlerta("Robado");
        var narcotrafico = new CategoriaAlerta("Narcotráfico");
        dbContext.CategoriasAlerta.Add(robado);
        dbContext.CategoriasAlerta.Add(narcotrafico);
        await dbContext.SaveChangesAsync();

        var handler = new RegistrarVehiculoCommandHandler(dbContext);

        var vehiculoId = await handler.Handle(
            new RegistrarVehiculoCommand(
                "Volkswagen", "Gol", "Gris", CertezaDominio.Incierto, AccionARealizar.Detener, "Comisaría 2°",
                "IAK 796", null, [robado.Id, narcotrafico.Id]),
            CancellationToken.None);

        var vehiculo = await dbContext.Vehiculos.FindAsync(vehiculoId);
        Assert.NotNull(vehiculo);
        Assert.Equal(EstadoVehiculo.Vigente, vehiculo.Estado);
        Assert.Equal(2, vehiculo.CategoriasAlertaIds.Count);
    }

    [Fact]
    public async Task Rechaza_una_categoria_de_alerta_inexistente()
    {
        var dbContext = new TestAppDbContext();
        var handler = new RegistrarVehiculoCommandHandler(dbContext);

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            new RegistrarVehiculoCommand(
                "Volkswagen", "Gol", "Gris", CertezaDominio.Incierto, AccionARealizar.Detener, "Comisaría 2°",
                null, null, [Guid.NewGuid()]),
            CancellationToken.None));
    }

    [Fact]
    public async Task Acepta_el_alta_sin_categorias_de_alerta()
    {
        var dbContext = new TestAppDbContext();
        var handler = new RegistrarVehiculoCommandHandler(dbContext);

        var vehiculoId = await handler.Handle(
            new RegistrarVehiculoCommand(
                "Volkswagen", "Gol", "Gris", CertezaDominio.Incierto, AccionARealizar.Detener, "Comisaría 2°",
                null, null, []),
            CancellationToken.None);

        var vehiculo = await dbContext.Vehiculos.FindAsync(vehiculoId);
        Assert.Empty(vehiculo!.CategoriasAlertaIds);
    }
}
