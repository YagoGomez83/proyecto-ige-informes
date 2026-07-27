using IGE.Informes.Application.Camaras.Commands.AsignarLocalidadACamara;
using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Camaras;

public class AsignarLocalidadACamaraCommandHandlerTests
{
    [Fact]
    public async Task Asigna_la_localidad_a_la_camara()
    {
        var dbContext = new TestAppDbContext();
        var camara = new Camara("EG 01", TipoCamara.Domo);
        var localidad = new Localidad("Estancia Grande");
        dbContext.Camaras.Add(camara);
        dbContext.Localidades.Add(localidad);
        await dbContext.SaveChangesAsync();

        var handler = new AsignarLocalidadACamaraCommandHandler(dbContext);

        await handler.Handle(new AsignarLocalidadACamaraCommand(camara.Id, localidad.Id), CancellationToken.None);

        var actualizada = await dbContext.Camaras.FindAsync(camara.Id);
        Assert.Equal(localidad.Id, actualizada!.LocalidadId);
    }

    [Fact]
    public async Task Rechaza_una_camara_inexistente()
    {
        var dbContext = new TestAppDbContext();
        var localidad = new Localidad("Estancia Grande");
        dbContext.Localidades.Add(localidad);
        await dbContext.SaveChangesAsync();

        var handler = new AsignarLocalidadACamaraCommandHandler(dbContext);

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            new AsignarLocalidadACamaraCommand(Guid.NewGuid(), localidad.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Rechaza_una_localidad_inexistente()
    {
        var dbContext = new TestAppDbContext();
        var camara = new Camara("EG 01", TipoCamara.Domo);
        dbContext.Camaras.Add(camara);
        await dbContext.SaveChangesAsync();

        var handler = new AsignarLocalidadACamaraCommandHandler(dbContext);

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            new AsignarLocalidadACamaraCommand(camara.Id, Guid.NewGuid()), CancellationToken.None));
    }
}
