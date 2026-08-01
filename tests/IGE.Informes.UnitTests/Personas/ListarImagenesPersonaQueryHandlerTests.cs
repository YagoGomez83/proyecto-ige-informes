using IGE.Informes.Application.Personas.Queries.ListarImagenesPersona;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Personas;

public class ListarImagenesPersonaQueryHandlerTests
{
    [Fact]
    public async Task Lista_las_imagenes_de_la_persona_con_url_prefirmada_y_sin_exponer_la_ruta_cruda()
    {
        var dbContext = new TestAppDbContext();
        var personaId = Guid.NewGuid();
        var imagen = new PersonaImagen(personaId, "clave/foto.jpg", Guid.NewGuid());
        dbContext.PersonaImagenes.Add(imagen);
        await dbContext.SaveChangesAsync();

        var fileStorage = new FakeFileStorage();
        var auditLogger = new FakeAuditLogger();
        var handler = new ListarImagenesPersonaQueryHandler(dbContext, fileStorage, auditLogger);

        var resultado = await handler.Handle(new ListarImagenesPersonaQuery(personaId), CancellationToken.None);

        var dto = Assert.Single(resultado);
        Assert.Equal(imagen.Id, dto.Id);
        Assert.Equal("https://fake-storage.local/clave/foto.jpg", dto.UrlDescarga);
    }

    [Fact]
    public async Task No_incluye_imagenes_de_otra_persona()
    {
        var dbContext = new TestAppDbContext();
        var personaBuscada = Guid.NewGuid();
        var otraPersona = Guid.NewGuid();
        dbContext.PersonaImagenes.Add(new PersonaImagen(personaBuscada, "clave/a.jpg", Guid.NewGuid()));
        dbContext.PersonaImagenes.Add(new PersonaImagen(otraPersona, "clave/b.jpg", Guid.NewGuid()));
        await dbContext.SaveChangesAsync();

        var handler = new ListarImagenesPersonaQueryHandler(dbContext, new FakeFileStorage(), new FakeAuditLogger());

        var resultado = await handler.Handle(new ListarImagenesPersonaQuery(personaBuscada), CancellationToken.None);

        Assert.Single(resultado);
    }

    [Fact]
    public async Task Registra_el_acceso_en_auditoria_sobre_persona()
    {
        var dbContext = new TestAppDbContext();
        var personaId = Guid.NewGuid();
        var auditLogger = new FakeAuditLogger();
        var handler = new ListarImagenesPersonaQueryHandler(dbContext, new FakeFileStorage(), auditLogger);

        await handler.Handle(new ListarImagenesPersonaQuery(personaId), CancellationToken.None);

        Assert.Contains(auditLogger.Registros,
            r => r.Accion == "Lectura" && r.Entidad == nameof(Persona) && r.EntidadId == personaId);
    }
}
