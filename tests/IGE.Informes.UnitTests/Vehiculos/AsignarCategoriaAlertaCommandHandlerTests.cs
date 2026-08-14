using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Vehiculos.Commands.AsignarCategoriaAlerta;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Vehiculos;

public class AsignarCategoriaAlertaCommandHandlerTests
{
    [Fact]
    public async Task Asigna_la_categoria_al_vehiculo()
    {
        var dbContext = new TestAppDbContext();
        var vehiculo = new Vehiculo("Volkswagen", "Gol", "Gris", CertezaDominio.Incierto, AccionARealizar.Detener, "Comisaría 2°", TipoVehiculo.Auto);
        var categoria = new CategoriaAlerta("Robado");
        dbContext.Vehiculos.Add(vehiculo);
        dbContext.CategoriasAlerta.Add(categoria);
        await dbContext.SaveChangesAsync();

        var handler = new AsignarCategoriaAlertaCommandHandler(dbContext);
        await handler.Handle(new AsignarCategoriaAlertaCommand(vehiculo.Id, categoria.Id), CancellationToken.None);

        var actualizado = await dbContext.Vehiculos.FindAsync(vehiculo.Id);
        Assert.Contains(categoria.Id, actualizado!.CategoriasAlertaIds);
    }

    [Fact]
    public async Task Rechaza_un_vehiculo_inexistente()
    {
        var dbContext = new TestAppDbContext();
        var categoria = new CategoriaAlerta("Robado");
        dbContext.CategoriasAlerta.Add(categoria);
        await dbContext.SaveChangesAsync();

        var handler = new AsignarCategoriaAlertaCommandHandler(dbContext);

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            new AsignarCategoriaAlertaCommand(Guid.NewGuid(), categoria.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Rechaza_una_categoria_de_alerta_inexistente()
    {
        var dbContext = new TestAppDbContext();
        var vehiculo = new Vehiculo("Volkswagen", "Gol", "Gris", CertezaDominio.Incierto, AccionARealizar.Detener, "Comisaría 2°", TipoVehiculo.Auto);
        dbContext.Vehiculos.Add(vehiculo);
        await dbContext.SaveChangesAsync();

        var handler = new AsignarCategoriaAlertaCommandHandler(dbContext);

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            new AsignarCategoriaAlertaCommand(vehiculo.Id, Guid.NewGuid()), CancellationToken.None));
    }
}
