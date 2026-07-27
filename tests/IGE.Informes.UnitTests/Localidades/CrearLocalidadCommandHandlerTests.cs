using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Localidades.Commands.CrearLocalidad;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Localidades;

public class CrearLocalidadCommandHandlerTests
{
    [Fact]
    public async Task Registra_una_localidad_nueva()
    {
        var dbContext = new TestAppDbContext();
        var handler = new CrearLocalidadCommandHandler(dbContext);

        var localidadId = await handler.Handle(new CrearLocalidadCommand("Estancia Grande"), CancellationToken.None);

        var localidad = await dbContext.Localidades.FindAsync(localidadId);
        Assert.Equal("Estancia Grande", localidad!.Nombre);
    }

    [Fact]
    public async Task Rechaza_un_nombre_duplicado()
    {
        var dbContext = new TestAppDbContext();
        dbContext.Localidades.Add(new Localidad("Estancia Grande"));
        await dbContext.SaveChangesAsync();

        var handler = new CrearLocalidadCommandHandler(dbContext);

        await Assert.ThrowsAsync<EntidadDuplicadaException>(() => handler.Handle(
            new CrearLocalidadCommand("Estancia Grande"), CancellationToken.None));
    }
}
