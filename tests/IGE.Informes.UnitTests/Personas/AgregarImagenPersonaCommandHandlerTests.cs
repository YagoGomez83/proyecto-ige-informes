using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Application.Personas.Commands.AgregarImagenPersona;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Personas;

public class AgregarImagenPersonaCommandHandlerTests
{
    private static Persona CrearPersona()
    {
        return new Persona(RolPersona.Testigo, nombre: "Juan Pérez");
    }

    [Fact]
    public async Task Sube_la_imagen_y_la_asocia_a_la_persona()
    {
        var dbContext = new TestAppDbContext();
        var persona = CrearPersona();
        dbContext.Personas.Add(persona);
        await dbContext.SaveChangesAsync();

        var usuarioId = Guid.NewGuid();
        var fileStorage = new FakeFileStorage();
        var handler = new AgregarImagenPersonaCommandHandler(
            dbContext, new FakeCurrentUserService(usuarioId), fileStorage, new FakeAntivirusScanner());

        var imagenId = await handler.Handle(
            new AgregarImagenPersonaCommand(persona.Id, [1, 2, 3], "foto.jpg", "image/jpeg"), CancellationToken.None);

        var imagen = await dbContext.PersonaImagenes.FindAsync(imagenId);
        Assert.NotNull(imagen);
        Assert.Equal(persona.Id, imagen!.PersonaId);
        Assert.Equal(usuarioId, imagen.SubidaPorUsuarioId);
        Assert.Single(fileStorage.ArchivosSubidos);
    }

    [Fact]
    public async Task Rechaza_una_persona_inexistente()
    {
        var dbContext = new TestAppDbContext();
        var handler = new AgregarImagenPersonaCommandHandler(
            dbContext, new FakeCurrentUserService(Guid.NewGuid()), new FakeFileStorage(), new FakeAntivirusScanner());

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            new AgregarImagenPersonaCommand(Guid.NewGuid(), [1, 2, 3], "foto.jpg", "image/jpeg"), CancellationToken.None));
    }

    [Fact]
    public async Task Rechaza_una_imagen_detectada_como_amenaza_y_no_la_sube()
    {
        var dbContext = new TestAppDbContext();
        var persona = CrearPersona();
        dbContext.Personas.Add(persona);
        await dbContext.SaveChangesAsync();

        var fileStorage = new FakeFileStorage();
        var antivirus = new FakeAntivirusScanner { ResultadoLimpio = false };
        var handler = new AgregarImagenPersonaCommandHandler(
            dbContext, new FakeCurrentUserService(Guid.NewGuid()), fileStorage, antivirus);

        await Assert.ThrowsAsync<ReglaDeNegocioVioladaException>(() => handler.Handle(
            new AgregarImagenPersonaCommand(persona.Id, [1, 2, 3], "foto.jpg", "image/jpeg"), CancellationToken.None));

        Assert.Empty(fileStorage.ArchivosSubidos);
        Assert.Empty(dbContext.PersonaImagenes);
    }

    [Fact]
    public async Task Propaga_la_excepcion_si_el_antivirus_no_esta_disponible()
    {
        // Fail-closed, mismo criterio que ConfirmarCargaInformeCommandHandler:
        // si ClamAV no responde, se rechaza la carga en vez de aceptarla sin
        // escanear.
        var dbContext = new TestAppDbContext();
        var persona = CrearPersona();
        dbContext.Personas.Add(persona);
        await dbContext.SaveChangesAsync();

        var fileStorage = new FakeFileStorage();
        var antivirus = new FakeAntivirusScanner { LanzarNoDisponible = true };
        var handler = new AgregarImagenPersonaCommandHandler(
            dbContext, new FakeCurrentUserService(Guid.NewGuid()), fileStorage, antivirus);

        await Assert.ThrowsAsync<AntivirusNoDisponibleException>(() => handler.Handle(
            new AgregarImagenPersonaCommand(persona.Id, [1, 2, 3], "foto.jpg", "image/jpeg"), CancellationToken.None));

        Assert.Empty(fileStorage.ArchivosSubidos);
    }
}
