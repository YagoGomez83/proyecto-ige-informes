using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Security;
using IGE.Informes.Application.Informes.Commands.EliminarInforme;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Informes.EliminarInforme;

/// <summary>
/// HU-21 · Borrado lógico de Informe, Caso de Análisis, Vehículo y Persona
/// (docs/epic-01-gestion-informes.md), Característica "Borrado lógico de un
/// Informe". El Command y el Handler todavía no existen — estos tests deben
/// fallar en rojo hasta que se implementen (TDD), ver
/// .claude/agents/gherkin-test-writer.md y docs/03-modelo-dominio.md,
/// "Borrado lógico de Informe, CasoAnalisis, Vehiculo y Persona".
/// </summary>
public class EliminarInformeCommandHandlerTests
{
    private static readonly Guid CasoAnalisisId = Guid.NewGuid();
    private static readonly Guid DependenciaDestinoId = Guid.NewGuid();
    private static readonly Guid AnalistaSolicitanteId = Guid.NewGuid();

    private static async Task<(TestAppDbContext DbContext, Informe Informe)> PrepararInformeEnBorradorAsync()
    {
        var dbContext = new TestAppDbContext();
        var informe = new Informe(
            "290/2026",
            new DateOnly(2026, 7, 21),
            CasoAnalisisId,
            DependenciaDestinoId,
            AnalistaSolicitanteId);

        dbContext.Informes.Add(informe);
        await dbContext.SaveChangesAsync();

        return (dbContext, informe);
    }

    [Fact]
    public async Task EliminarInforme_EnBorrador_DebeMarcarloComoEliminado()
    {
        var (dbContext, informe) = await PrepararInformeEnBorradorAsync();
        var handler = new EliminarInformeCommandHandler(dbContext);

        await handler.Handle(new EliminarInformeCommand(informe.Id), CancellationToken.None);

        var actualizado = await dbContext.Informes.FindAsync(informe.Id);
        Assert.NotNull(actualizado);
        Assert.True(actualizado.Eliminado);
        Assert.NotNull(actualizado.FechaEliminacion);
    }

    [Fact]
    public async Task EliminarInforme_InformePublicado_DebeRechazarPublicacionInmutable()
    {
        // AgregarFirmante+Publicar() se hacen ANTES del primer SaveChanges
        // (no como Update posterior a una entidad ya persistida) — EF Core
        // InMemory tiene una limitación conocida con Add sobre una owned
        // collection (InformeAnalista) de una entidad padre ya trackeada,
        // ver PublicarInformeCommandHandlerTests y memoria del proyecto
        // (feedback_efcore_ownsmany_added_como_modified).
        var dbContext = new TestAppDbContext();
        var informe = new Informe(
            "290/2026",
            new DateOnly(2026, 7, 21),
            CasoAnalisisId,
            DependenciaDestinoId,
            AnalistaSolicitanteId,
            causaId: Guid.NewGuid());
        informe.AgregarFirmante(AnalistaSolicitanteId);
        informe.Publicar();

        dbContext.Informes.Add(informe);
        await dbContext.SaveChangesAsync();

        var handler = new EliminarInformeCommandHandler(dbContext);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            new EliminarInformeCommand(informe.Id), CancellationToken.None));

        var todaviaExiste = await dbContext.Informes.FindAsync(informe.Id);
        Assert.NotNull(todaviaExiste);
        Assert.False(todaviaExiste.Eliminado);
    }

    [Fact]
    public async Task EliminarInforme_InformeInexistente_DebeRechazarConEntidadNoEncontrada()
    {
        var dbContext = new TestAppDbContext();
        var handler = new EliminarInformeCommandHandler(dbContext);

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            new EliminarInformeCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public void EliminarInformeCommand_DeclaraAutorizacion_ParaSupervisorYAdmin()
    {
        var atributo = typeof(EliminarInformeCommand)
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
