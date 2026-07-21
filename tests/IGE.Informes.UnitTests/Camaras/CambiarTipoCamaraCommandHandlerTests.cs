using IGE.Informes.Application.Camaras.Commands.CambiarTipoCamara;
using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Camaras;

public class CambiarTipoCamaraCommandHandlerTests
{
    [Fact]
    public async Task Cambia_el_tipo_de_la_camara()
    {
        var dbContext = new TestAppDbContext();
        var camara = new Camara("SL 18", TipoCamara.Domo);
        dbContext.Camaras.Add(camara);
        await dbContext.SaveChangesAsync();

        var handler = new CambiarTipoCamaraCommandHandler(dbContext);
        await handler.Handle(new CambiarTipoCamaraCommand(camara.Id, TipoCamara.Lpr), CancellationToken.None);

        var actualizada = await dbContext.Camaras.FindAsync(camara.Id);
        Assert.Equal(TipoCamara.Lpr, actualizada!.Tipo);
    }

    [Fact]
    public async Task Rechaza_una_camara_inexistente()
    {
        var dbContext = new TestAppDbContext();
        var handler = new CambiarTipoCamaraCommandHandler(dbContext);

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            new CambiarTipoCamaraCommand(Guid.NewGuid(), TipoCamara.Lpr), CancellationToken.None));
    }
}
