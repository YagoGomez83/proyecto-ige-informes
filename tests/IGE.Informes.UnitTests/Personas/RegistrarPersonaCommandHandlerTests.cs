using IGE.Informes.Application.Personas.Commands.RegistrarPersona;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Personas;

public class RegistrarPersonaCommandHandlerTests
{
    [Fact]
    public async Task Registra_una_persona_identificada()
    {
        var dbContext = new TestAppDbContext();
        var handler = new RegistrarPersonaCommandHandler(dbContext);

        var personaId = await handler.Handle(
            new RegistrarPersonaCommand(RolPersona.Sospechoso, "Juan Pérez", "30123456", null),
            CancellationToken.None);

        var persona = await dbContext.Personas.FindAsync(personaId);
        Assert.Equal("Juan Pérez", persona!.Nombre);
    }

    [Fact]
    public async Task Registra_una_persona_sin_identificar_con_caracteristicas()
    {
        var dbContext = new TestAppDbContext();
        var handler = new RegistrarPersonaCommandHandler(dbContext);

        var personaId = await handler.Handle(
            new RegistrarPersonaCommand(RolPersona.Testigo, null, null, "Mujer, 1.60m"),
            CancellationToken.None);

        var persona = await dbContext.Personas.FindAsync(personaId);
        Assert.Null(persona!.Nombre);
        Assert.Equal("Mujer, 1.60m", persona.Caracteristicas);
    }
}
