using IGE.Informes.Application.Barrios.Commands.CrearBarrio;
using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Barrios;

public class CrearBarrioCommandHandlerTests
{
    [Fact]
    public async Task Registra_un_barrio_nuevo()
    {
        var dbContext = new TestAppDbContext();
        var handler = new CrearBarrioCommandHandler(dbContext);

        var barrioId = await handler.Handle(new CrearBarrioCommand("Barrio Norte"), CancellationToken.None);

        var barrio = await dbContext.Barrios.FindAsync(barrioId);
        Assert.Equal("Barrio Norte", barrio!.Nombre);
    }

    [Fact]
    public async Task Rechaza_un_nombre_duplicado()
    {
        var dbContext = new TestAppDbContext();
        dbContext.Barrios.Add(new Barrio("Barrio Norte"));
        await dbContext.SaveChangesAsync();

        var handler = new CrearBarrioCommandHandler(dbContext);

        await Assert.ThrowsAsync<EntidadDuplicadaException>(() => handler.Handle(
            new CrearBarrioCommand("Barrio Norte"), CancellationToken.None));
    }
}
