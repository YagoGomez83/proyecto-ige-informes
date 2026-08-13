using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Security;
using IGE.Informes.Application.Personas.Commands.EliminarPersona;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Personas.EliminarPersona;

/// <summary>
/// HU-21 · Borrado lógico de Informe, Caso de Análisis, Vehículo y Persona
/// (docs/epic-01-gestion-informes.md), Característica "Borrado lógico de
/// una Persona". El Command y el Handler todavía no existen — estos tests
/// deben fallar en rojo hasta que se implementen (TDD), ver
/// .claude/agents/gherkin-test-writer.md y docs/03-modelo-dominio.md,
/// "Borrado lógico de Informe, CasoAnalisis, Vehiculo y Persona".
/// </summary>
public class EliminarPersonaCommandHandlerTests
{
    private static async Task<(TestAppDbContext DbContext, Persona Persona)> PrepararAsync()
    {
        var dbContext = new TestAppDbContext();
        var persona = new Persona(RolPersona.Sospechoso, "Juan Pérez", "30123456");
        dbContext.Personas.Add(persona);
        await dbContext.SaveChangesAsync();

        return (dbContext, persona);
    }

    [Fact]
    public async Task EliminarPersona_DelCatalogo_DebeMarcarlaComoEliminada()
    {
        var (dbContext, persona) = await PrepararAsync();
        var handler = new EliminarPersonaCommandHandler(dbContext);

        await handler.Handle(new EliminarPersonaCommand(persona.Id), CancellationToken.None);

        var actualizada = await dbContext.Personas.FindAsync(persona.Id);
        Assert.NotNull(actualizada);
        Assert.True(actualizada.Eliminado);
        Assert.NotNull(actualizada.FechaEliminacion);
    }

    [Fact]
    public async Task EliminarPersona_PersonaInexistente_DebeRechazarConEntidadNoEncontrada()
    {
        var dbContext = new TestAppDbContext();
        var handler = new EliminarPersonaCommandHandler(dbContext);

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            new EliminarPersonaCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public void EliminarPersonaCommand_DeclaraAutorizacion_ParaSupervisorYAdmin()
    {
        var atributo = typeof(EliminarPersonaCommand)
            .GetCustomAttributes(typeof(AutorizarAttribute), inherit: true)
            .Cast<AutorizarAttribute>()
            .SingleOrDefault();

        Assert.NotNull(atributo);
        Assert.Equal(2, atributo.Roles.Count);
        Assert.Contains(Roles.Supervisor, atributo.Roles);
        Assert.Contains(Roles.Admin, atributo.Roles);
        Assert.DoesNotContain(Roles.Analista, atributo.Roles);
    }
}
