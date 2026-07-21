using IGE.Informes.Application.Personas.Queries.ObtenerPersonaPorId;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Personas;

public class ObtenerPersonaPorIdQueryHandlerTests
{
    [Fact]
    public async Task Devuelve_la_persona_y_registra_la_lectura_en_AuditLog()
    {
        var dbContext = new TestAppDbContext();
        var persona = new Persona(RolPersona.Sospechoso, "Juan Pérez", "30123456");
        dbContext.Personas.Add(persona);
        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new ObtenerPersonaPorIdQueryHandler(dbContext, auditLogger);

        var dto = await handler.Handle(new ObtenerPersonaPorIdQuery(persona.Id), CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal("Juan Pérez", dto.Nombre);
        Assert.Single(auditLogger.Registros);
        Assert.Equal(("Lectura", nameof(Persona), persona.Id), auditLogger.Registros[0]);
    }

    [Fact]
    public async Task Persona_inexistente_devuelve_null_pero_igual_registra_el_intento_de_lectura()
    {
        var dbContext = new TestAppDbContext();
        var auditLogger = new FakeAuditLogger();
        var handler = new ObtenerPersonaPorIdQueryHandler(dbContext, auditLogger);

        var dto = await handler.Handle(new ObtenerPersonaPorIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(dto);
        Assert.Single(auditLogger.Registros);
    }
}
