using IGE.Informes.Application.Camaras.Commands.RegistrarCamara;
using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Camaras;

public class RegistrarCamaraCommandHandlerTests
{
    [Fact]
    public async Task Registra_una_camara_nueva()
    {
        var dbContext = new TestAppDbContext();
        var handler = new RegistrarCamaraCommandHandler(dbContext);

        var camaraId = await handler.Handle(
            new RegistrarCamaraCommand("SL 18", TipoCamara.Domo, "Av. Illia y San Martín"),
            CancellationToken.None);

        var camara = await dbContext.Camaras.FindAsync(camaraId);
        Assert.Equal("SL 18", camara!.Codigo);
    }

    [Fact]
    public async Task Rechaza_un_codigo_duplicado()
    {
        var dbContext = new TestAppDbContext();
        dbContext.Camaras.Add(new Camara("SL 18", TipoCamara.Domo));
        await dbContext.SaveChangesAsync();

        var handler = new RegistrarCamaraCommandHandler(dbContext);

        await Assert.ThrowsAsync<EntidadDuplicadaException>(() => handler.Handle(
            new RegistrarCamaraCommand("SL 18", TipoCamara.Lpr, null), CancellationToken.None));
    }
}
