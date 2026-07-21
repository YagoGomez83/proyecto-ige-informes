using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Personas.Commands.CambiarRolPersona;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Personas;

public class CambiarRolPersonaCommandHandlerTests
{
    [Fact]
    public async Task Cambia_el_rol_de_la_persona()
    {
        var dbContext = new TestAppDbContext();
        var persona = new Persona(RolPersona.Denunciante, "Juan Pérez");
        dbContext.Personas.Add(persona);
        await dbContext.SaveChangesAsync();

        var handler = new CambiarRolPersonaCommandHandler(dbContext);
        await handler.Handle(new CambiarRolPersonaCommand(persona.Id, RolPersona.Testigo), CancellationToken.None);

        var actualizada = await dbContext.Personas.FindAsync(persona.Id);
        Assert.Equal(RolPersona.Testigo, actualizada!.Rol);
    }

    [Fact]
    public async Task Rechaza_una_persona_inexistente()
    {
        var dbContext = new TestAppDbContext();
        var handler = new CambiarRolPersonaCommandHandler(dbContext);

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            new CambiarRolPersonaCommand(Guid.NewGuid(), RolPersona.Testigo), CancellationToken.None));
    }
}
