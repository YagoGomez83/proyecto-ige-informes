using IGE.Informes.Application.Vehiculos.Queries.ListarImagenesVehiculo;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Vehiculos;

public class ListarImagenesVehiculoQueryHandlerTests
{
    [Fact]
    public async Task Lista_las_imagenes_del_vehiculo_con_url_prefirmada_y_sin_exponer_la_ruta_cruda()
    {
        var dbContext = new TestAppDbContext();
        var vehiculoId = Guid.NewGuid();
        var imagen = new VehiculoImagen(vehiculoId, "clave/foto.jpg", Guid.NewGuid());
        dbContext.VehiculoImagenes.Add(imagen);
        await dbContext.SaveChangesAsync();

        var fileStorage = new FakeFileStorage();
        var auditLogger = new FakeAuditLogger();
        var handler = new ListarImagenesVehiculoQueryHandler(dbContext, fileStorage, auditLogger);

        var resultado = await handler.Handle(new ListarImagenesVehiculoQuery(vehiculoId), CancellationToken.None);

        var dto = Assert.Single(resultado);
        Assert.Equal(imagen.Id, dto.Id);
        Assert.Equal("https://fake-storage.local/clave/foto.jpg", dto.UrlDescarga);
    }

    [Fact]
    public async Task No_incluye_imagenes_de_otro_vehiculo()
    {
        var dbContext = new TestAppDbContext();
        var vehiculoBuscado = Guid.NewGuid();
        var otroVehiculo = Guid.NewGuid();
        dbContext.VehiculoImagenes.Add(new VehiculoImagen(vehiculoBuscado, "clave/a.jpg", Guid.NewGuid()));
        dbContext.VehiculoImagenes.Add(new VehiculoImagen(otroVehiculo, "clave/b.jpg", Guid.NewGuid()));
        await dbContext.SaveChangesAsync();

        var handler = new ListarImagenesVehiculoQueryHandler(dbContext, new FakeFileStorage(), new FakeAuditLogger());

        var resultado = await handler.Handle(new ListarImagenesVehiculoQuery(vehiculoBuscado), CancellationToken.None);

        Assert.Single(resultado);
    }

    [Fact]
    public async Task Registra_el_acceso_en_auditoria_sobre_vehiculo()
    {
        var dbContext = new TestAppDbContext();
        var vehiculoId = Guid.NewGuid();
        var auditLogger = new FakeAuditLogger();
        var handler = new ListarImagenesVehiculoQueryHandler(dbContext, new FakeFileStorage(), auditLogger);

        await handler.Handle(new ListarImagenesVehiculoQuery(vehiculoId), CancellationToken.None);

        Assert.Contains(auditLogger.Registros,
            r => r.Accion == "Lectura" && r.Entidad == nameof(Vehiculo) && r.EntidadId == vehiculoId);
    }
}
