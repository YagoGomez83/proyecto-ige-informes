using IGE.Informes.Application.Camaras.Commands.CompletarUbicacionCamara;
using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Camaras;

public class CompletarUbicacionCamaraCommandHandlerTests
{
    [Fact]
    public async Task Completa_la_ubicacion_de_una_camara_pendiente()
    {
        var dbContext = new TestAppDbContext();
        var camara = new Camara("JK 51", TipoCamara.Lpr);
        dbContext.Camaras.Add(camara);
        await dbContext.SaveChangesAsync();

        var handler = new CompletarUbicacionCamaraCommandHandler(dbContext);
        await handler.Handle(new CompletarUbicacionCamaraCommand(camara.Id, "Ruta 7 km 12"), CancellationToken.None);

        var actualizada = await dbContext.Camaras.FindAsync(camara.Id);
        Assert.Equal("Ruta 7 km 12", actualizada!.Ubicacion);
    }

    [Fact]
    public async Task Rechaza_una_camara_inexistente()
    {
        var dbContext = new TestAppDbContext();
        var handler = new CompletarUbicacionCamaraCommandHandler(dbContext);

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            new CompletarUbicacionCamaraCommand(Guid.NewGuid(), "Ruta 7 km 12"), CancellationToken.None));
    }
}
