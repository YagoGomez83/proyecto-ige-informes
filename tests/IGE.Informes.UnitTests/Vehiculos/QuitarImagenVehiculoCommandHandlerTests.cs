using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Vehiculos.Commands.QuitarImagenVehiculo;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Vehiculos;

public class QuitarImagenVehiculoCommandHandlerTests
{
    [Fact]
    public async Task Elimina_la_imagen_del_storage_y_de_la_base()
    {
        var dbContext = new TestAppDbContext();
        var imagen = new VehiculoImagen(Guid.NewGuid(), "clave/foto.jpg", Guid.NewGuid());
        dbContext.VehiculoImagenes.Add(imagen);
        await dbContext.SaveChangesAsync();

        var fileStorage = new FakeFileStorage();
        var handler = new QuitarImagenVehiculoCommandHandler(dbContext, fileStorage);

        await handler.Handle(new QuitarImagenVehiculoCommand(imagen.Id), CancellationToken.None);

        Assert.Empty(dbContext.VehiculoImagenes);
        Assert.Contains("clave/foto.jpg", fileStorage.ArchivosEliminados);
    }

    [Fact]
    public async Task Rechaza_una_imagen_inexistente()
    {
        var dbContext = new TestAppDbContext();
        var handler = new QuitarImagenVehiculoCommandHandler(dbContext, new FakeFileStorage());

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            new QuitarImagenVehiculoCommand(Guid.NewGuid()), CancellationToken.None));
    }
}
