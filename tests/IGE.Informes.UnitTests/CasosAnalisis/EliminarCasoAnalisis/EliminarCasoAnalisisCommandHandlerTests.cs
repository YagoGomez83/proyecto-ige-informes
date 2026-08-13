using IGE.Informes.Application.CasosAnalisis.Commands.EliminarCasoAnalisis;
using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Security;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.CasosAnalisis.EliminarCasoAnalisis;

/// <summary>
/// HU-21 · Borrado lógico de Informe, Caso de Análisis, Vehículo y Persona
/// (docs/epic-01-gestion-informes.md), Característica "Borrado lógico de un
/// Caso de Análisis". El Command y el Handler todavía no existen — estos
/// tests deben fallar en rojo hasta que se implementen (TDD), ver
/// .claude/agents/gherkin-test-writer.md y docs/03-modelo-dominio.md,
/// "Borrado lógico de Informe, CasoAnalisis, Vehiculo y Persona".
/// </summary>
public class EliminarCasoAnalisisCommandHandlerTests
{
    private static async Task<(TestAppDbContext DbContext, CasoAnalisis Caso)> PrepararCasoAsync()
    {
        var dbContext = new TestAppDbContext();
        var caso = new CasoAnalisis(new DateOnly(2026, 7, 21), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        dbContext.CasosAnalisis.Add(caso);
        await dbContext.SaveChangesAsync();

        return (dbContext, caso);
    }

    [Fact]
    public async Task EliminarCasoAnalisis_EnEstadoPendiente_DebeMarcarloComoEliminado()
    {
        var (dbContext, caso) = await PrepararCasoAsync();
        var handler = new EliminarCasoAnalisisCommandHandler(dbContext);

        await handler.Handle(new EliminarCasoAnalisisCommand(caso.Id), CancellationToken.None);

        var actualizado = await dbContext.CasosAnalisis.FindAsync(caso.Id);
        Assert.NotNull(actualizado);
        Assert.True(actualizado.Eliminado);
        Assert.NotNull(actualizado.FechaEliminacion);
    }

    [Fact]
    public async Task EliminarCasoAnalisis_CasoCerrado_NoBloqueaLaEliminacion()
    {
        var (dbContext, caso) = await PrepararCasoAsync();
        caso.CerrarConResultado(ResultadoCaso.Positivo);
        await dbContext.SaveChangesAsync();

        var handler = new EliminarCasoAnalisisCommandHandler(dbContext);

        await handler.Handle(new EliminarCasoAnalisisCommand(caso.Id), CancellationToken.None);

        var actualizado = await dbContext.CasosAnalisis.FindAsync(caso.Id);
        Assert.NotNull(actualizado);
        Assert.True(actualizado.Eliminado);
        Assert.Equal(EstadoCaso.Cerrado, actualizado.Estado);
    }

    [Fact]
    public async Task EliminarCasoAnalisis_CasoInexistente_DebeRechazarConEntidadNoEncontrada()
    {
        var dbContext = new TestAppDbContext();
        var handler = new EliminarCasoAnalisisCommandHandler(dbContext);

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            new EliminarCasoAnalisisCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public void EliminarCasoAnalisisCommand_DeclaraAutorizacion_ParaSupervisorYAdmin()
    {
        var atributo = typeof(EliminarCasoAnalisisCommand)
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
