using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Personas.Commands.IdentificarPersona;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Personas;

public class IdentificarPersonaCommandHandlerTests
{
    [Fact]
    public async Task Completa_nombre_y_dni_de_una_persona_sin_identificar()
    {
        var dbContext = new TestAppDbContext();
        var persona = new Persona(RolPersona.Sospechoso, caracteristicas: "Hombre, 1.80m");
        dbContext.Personas.Add(persona);
        await dbContext.SaveChangesAsync();

        var handler = new IdentificarPersonaCommandHandler(dbContext);
        await handler.Handle(new IdentificarPersonaCommand(persona.Id, "Juan Pérez", "30123456"), CancellationToken.None);

        var actualizada = await dbContext.Personas.FindAsync(persona.Id);
        Assert.Equal("Juan Pérez", actualizada!.Nombre);
        Assert.Equal("30123456", actualizada.Dni);
    }

    [Fact]
    public async Task Rechaza_una_persona_inexistente()
    {
        var dbContext = new TestAppDbContext();
        var handler = new IdentificarPersonaCommandHandler(dbContext);

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            new IdentificarPersonaCommand(Guid.NewGuid(), "Juan Pérez", null), CancellationToken.None));
    }
}
