using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Application.Vehiculos.Commands.AgregarImagenVehiculo;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Vehiculos;

public class AgregarImagenVehiculoCommandHandlerTests
{
    private static Vehiculo CrearVehiculo()
    {
        return new Vehiculo("Ford", "Fiesta", "Gris", CertezaDominio.Confirmado, AccionARealizar.Identificar, "Comisaría 2°", TipoVehiculo.Auto);
    }

    [Fact]
    public async Task Sube_la_imagen_y_la_asocia_al_vehiculo()
    {
        var dbContext = new TestAppDbContext();
        var vehiculo = CrearVehiculo();
        dbContext.Vehiculos.Add(vehiculo);
        await dbContext.SaveChangesAsync();

        var usuarioId = Guid.NewGuid();
        var fileStorage = new FakeFileStorage();
        var handler = new AgregarImagenVehiculoCommandHandler(
            dbContext, new FakeCurrentUserService(usuarioId), fileStorage, new FakeAntivirusScanner());

        var imagenId = await handler.Handle(
            new AgregarImagenVehiculoCommand(vehiculo.Id, [1, 2, 3], "foto.jpg", "image/jpeg"), CancellationToken.None);

        var imagen = await dbContext.VehiculoImagenes.FindAsync(imagenId);
        Assert.NotNull(imagen);
        Assert.Equal(vehiculo.Id, imagen!.VehiculoId);
        Assert.Equal(usuarioId, imagen.SubidaPorUsuarioId);
        Assert.Single(fileStorage.ArchivosSubidos);
    }

    [Fact]
    public async Task Rechaza_un_vehiculo_inexistente()
    {
        var dbContext = new TestAppDbContext();
        var handler = new AgregarImagenVehiculoCommandHandler(
            dbContext, new FakeCurrentUserService(Guid.NewGuid()), new FakeFileStorage(), new FakeAntivirusScanner());

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            new AgregarImagenVehiculoCommand(Guid.NewGuid(), [1, 2, 3], "foto.jpg", "image/jpeg"), CancellationToken.None));
    }

    [Fact]
    public async Task Rechaza_una_imagen_detectada_como_amenaza_y_no_la_sube()
    {
        var dbContext = new TestAppDbContext();
        var vehiculo = CrearVehiculo();
        dbContext.Vehiculos.Add(vehiculo);
        await dbContext.SaveChangesAsync();

        var fileStorage = new FakeFileStorage();
        var antivirus = new FakeAntivirusScanner { ResultadoLimpio = false };
        var handler = new AgregarImagenVehiculoCommandHandler(
            dbContext, new FakeCurrentUserService(Guid.NewGuid()), fileStorage, antivirus);

        await Assert.ThrowsAsync<ReglaDeNegocioVioladaException>(() => handler.Handle(
            new AgregarImagenVehiculoCommand(vehiculo.Id, [1, 2, 3], "foto.jpg", "image/jpeg"), CancellationToken.None));

        Assert.Empty(fileStorage.ArchivosSubidos);
        Assert.Empty(dbContext.VehiculoImagenes);
    }

    [Fact]
    public async Task Propaga_la_excepcion_si_el_antivirus_no_esta_disponible()
    {
        // Fail-closed, mismo criterio que ConfirmarCargaInformeCommandHandler:
        // si ClamAV no responde, se rechaza la carga en vez de aceptarla sin
        // escanear.
        var dbContext = new TestAppDbContext();
        var vehiculo = CrearVehiculo();
        dbContext.Vehiculos.Add(vehiculo);
        await dbContext.SaveChangesAsync();

        var fileStorage = new FakeFileStorage();
        var antivirus = new FakeAntivirusScanner { LanzarNoDisponible = true };
        var handler = new AgregarImagenVehiculoCommandHandler(
            dbContext, new FakeCurrentUserService(Guid.NewGuid()), fileStorage, antivirus);

        await Assert.ThrowsAsync<AntivirusNoDisponibleException>(() => handler.Handle(
            new AgregarImagenVehiculoCommand(vehiculo.Id, [1, 2, 3], "foto.jpg", "image/jpeg"), CancellationToken.None));

        Assert.Empty(fileStorage.ArchivosSubidos);
    }
}
