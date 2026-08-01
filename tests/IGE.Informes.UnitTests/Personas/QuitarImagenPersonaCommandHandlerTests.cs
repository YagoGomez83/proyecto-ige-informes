using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Personas.Commands.QuitarImagenPersona;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Personas;

public class QuitarImagenPersonaCommandHandlerTests
{
    [Fact]
    public async Task Elimina_la_imagen_del_storage_y_de_la_base()
    {
        var dbContext = new TestAppDbContext();
        var imagen = new PersonaImagen(Guid.NewGuid(), "clave/foto.jpg", Guid.NewGuid());
        dbContext.PersonaImagenes.Add(imagen);
        await dbContext.SaveChangesAsync();

        var fileStorage = new FakeFileStorage();
        var handler = new QuitarImagenPersonaCommandHandler(dbContext, fileStorage);

        await handler.Handle(new QuitarImagenPersonaCommand(imagen.Id), CancellationToken.None);

        Assert.Empty(dbContext.PersonaImagenes);
        Assert.Contains("clave/foto.jpg", fileStorage.ArchivosEliminados);
    }

    [Fact]
    public async Task Rechaza_una_imagen_inexistente()
    {
        var dbContext = new TestAppDbContext();
        var handler = new QuitarImagenPersonaCommandHandler(dbContext, new FakeFileStorage());

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            new QuitarImagenPersonaCommand(Guid.NewGuid()), CancellationToken.None));
    }
}
