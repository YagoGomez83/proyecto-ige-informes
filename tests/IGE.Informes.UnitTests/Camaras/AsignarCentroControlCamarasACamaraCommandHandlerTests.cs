using IGE.Informes.Application.Camaras.Commands.AsignarCentroControlCamarasACamara;
using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Camaras;

public class AsignarCentroControlCamarasACamaraCommandHandlerTests
{
    [Fact]
    public async Task Asigna_el_centro_de_control_a_la_camara()
    {
        var dbContext = new TestAppDbContext();
        var camara = new Camara("SL 18", TipoCamara.Domo);
        var centro = new CentroControlCamaras("CCCSL", "Centro de Control de Cámaras San Luis");
        dbContext.Camaras.Add(camara);
        dbContext.CentrosControlCamaras.Add(centro);
        await dbContext.SaveChangesAsync();

        var handler = new AsignarCentroControlCamarasACamaraCommandHandler(dbContext);

        await handler.Handle(
            new AsignarCentroControlCamarasACamaraCommand(camara.Id, centro.Id), CancellationToken.None);

        var actualizada = await dbContext.Camaras.FindAsync(camara.Id);
        Assert.Equal(centro.Id, actualizada!.CentroControlCamarasId);
    }

    [Fact]
    public async Task Rechaza_una_camara_inexistente()
    {
        var dbContext = new TestAppDbContext();
        var centro = new CentroControlCamaras("CCCSL", "Centro de Control de Cámaras San Luis");
        dbContext.CentrosControlCamaras.Add(centro);
        await dbContext.SaveChangesAsync();

        var handler = new AsignarCentroControlCamarasACamaraCommandHandler(dbContext);

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            new AsignarCentroControlCamarasACamaraCommand(Guid.NewGuid(), centro.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Rechaza_un_centro_de_control_inexistente()
    {
        var dbContext = new TestAppDbContext();
        var camara = new Camara("SL 18", TipoCamara.Domo);
        dbContext.Camaras.Add(camara);
        await dbContext.SaveChangesAsync();

        var handler = new AsignarCentroControlCamarasACamaraCommandHandler(dbContext);

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            new AsignarCentroControlCamarasACamaraCommand(camara.Id, Guid.NewGuid()), CancellationToken.None));
    }
}
