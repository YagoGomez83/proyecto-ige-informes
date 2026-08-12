using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Dependencias.Commands.EliminarDependencia;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Dependencias.EliminarDependencia;

/// <summary>
/// Tests de los escenarios Gherkin de "Eliminar una Dependencia sin uso"
/// (extensión de HU-11, docs/epic-04-gestion-catalogos.md). El Command y el
/// Handler todavía no existen — estos tests deben fallar en rojo hasta que
/// se implementen (TDD), ver .claude/agents/gherkin-test-writer.md.
/// </summary>
public class EliminarDependenciaCommandHandlerTests
{
    [Fact]
    public async Task EliminarDependencia_SinNingunaReferencia_DebeEliminarla()
    {
        var dbContext = new TestAppDbContext();
        var dependencia = new Dependencia("División Investigaciones", TipoDependencia.Division);
        dbContext.Dependencias.Add(dependencia);
        await dbContext.SaveChangesAsync();

        var handler = new EliminarDependenciaCommandHandler(dbContext);

        await handler.Handle(new EliminarDependenciaCommand(dependencia.Id), CancellationToken.None);

        var eliminada = await dbContext.Dependencias.FindAsync(dependencia.Id);
        Assert.Null(eliminada);
    }

    [Fact]
    public async Task EliminarDependencia_ReferenciadaPorInformeComoDependenciaDestino_DebeRechazarEliminacion()
    {
        var dbContext = new TestAppDbContext();
        var dependencia = new Dependencia("Departamento Investigaciones", TipoDependencia.Division);
        dbContext.Dependencias.Add(dependencia);
        await dbContext.SaveChangesAsync();

        var informe = Informe.CrearMigrado(
            "290/2026",
            DateOnly.FromDateTime(DateTime.Today),
            dependencia.Id,
            Guid.NewGuid());
        dbContext.Informes.Add(informe);
        await dbContext.SaveChangesAsync();

        var handler = new EliminarDependenciaCommandHandler(dbContext);

        await Assert.ThrowsAsync<ReglaDeNegocioVioladaException>(() => handler.Handle(
            new EliminarDependenciaCommand(dependencia.Id), CancellationToken.None));

        var todaviaExiste = await dbContext.Dependencias.FindAsync(dependencia.Id);
        Assert.NotNull(todaviaExiste);
    }

    [Fact]
    public async Task EliminarDependencia_ReferenciadaPorCamara_DebeRechazarEliminacion()
    {
        var dbContext = new TestAppDbContext();
        var dependencia = new Dependencia("Comisaría Seccional Primera", TipoDependencia.Comisaria);
        dbContext.Dependencias.Add(dependencia);
        await dbContext.SaveChangesAsync();

        var camara = new Camara("SL 18", TipoCamara.Domo, dependenciaId: dependencia.Id);
        dbContext.Camaras.Add(camara);
        await dbContext.SaveChangesAsync();

        var handler = new EliminarDependenciaCommandHandler(dbContext);

        await Assert.ThrowsAsync<ReglaDeNegocioVioladaException>(() => handler.Handle(
            new EliminarDependenciaCommand(dependencia.Id), CancellationToken.None));

        var todaviaExiste = await dbContext.Dependencias.FindAsync(dependencia.Id);
        Assert.NotNull(todaviaExiste);
    }

    [Fact]
    public async Task EliminarDependencia_ReferenciadaPorCasoAnalisis_DebeRechazarEliminacion()
    {
        var dbContext = new TestAppDbContext();
        var dependencia = new Dependencia("Comisaría Seccional Segunda", TipoDependencia.Comisaria);
        dbContext.Dependencias.Add(dependencia);
        await dbContext.SaveChangesAsync();

        var caso = new CasoAnalisis(
            DateOnly.FromDateTime(DateTime.Today),
            dependencia.Id,
            Guid.NewGuid(),
            Guid.NewGuid());
        dbContext.CasosAnalisis.Add(caso);
        await dbContext.SaveChangesAsync();

        var handler = new EliminarDependenciaCommandHandler(dbContext);

        await Assert.ThrowsAsync<ReglaDeNegocioVioladaException>(() => handler.Handle(
            new EliminarDependenciaCommand(dependencia.Id), CancellationToken.None));

        var todaviaExiste = await dbContext.Dependencias.FindAsync(dependencia.Id);
        Assert.NotNull(todaviaExiste);
    }

    [Fact]
    public async Task EliminarDependencia_ReferenciadaPorMigracionPendiente_DebeRechazarEliminacion()
    {
        var dbContext = new TestAppDbContext();
        var dependencia = new Dependencia("Comisaría Seccional Tercera", TipoDependencia.Comisaria);
        dbContext.Dependencias.Add(dependencia);
        await dbContext.SaveChangesAsync();

        var migracionPendiente = new MigracionPendiente(
            idRegistro: null,
            pdfPath: "migraciones/290-2026.pdf",
            dependenciaDestinoId: dependencia.Id,
            usuarioMigradorId: Guid.NewGuid());
        dbContext.MigracionesPendientes.Add(migracionPendiente);
        await dbContext.SaveChangesAsync();

        var handler = new EliminarDependenciaCommandHandler(dbContext);

        await Assert.ThrowsAsync<ReglaDeNegocioVioladaException>(() => handler.Handle(
            new EliminarDependenciaCommand(dependencia.Id), CancellationToken.None));

        var todaviaExiste = await dbContext.Dependencias.FindAsync(dependencia.Id);
        Assert.NotNull(todaviaExiste);
    }

    [Fact]
    public async Task EliminarDependencia_EsUnidadRegionalDeOtraDependencia_DebeRechazarEliminacion()
    {
        var dbContext = new TestAppDbContext();
        var unidadRegional = new Dependencia("UR 1", TipoDependencia.UnidadRegional);
        var comisaria = new Dependencia("Comisaría 2°", TipoDependencia.Comisaria);
        dbContext.Dependencias.Add(unidadRegional);
        dbContext.Dependencias.Add(comisaria);
        await dbContext.SaveChangesAsync();

        comisaria.AsignarUnidadRegional(unidadRegional.Id);
        await dbContext.SaveChangesAsync();

        var handler = new EliminarDependenciaCommandHandler(dbContext);

        await Assert.ThrowsAsync<ReglaDeNegocioVioladaException>(() => handler.Handle(
            new EliminarDependenciaCommand(unidadRegional.Id), CancellationToken.None));

        var todaviaExiste = await dbContext.Dependencias.FindAsync(unidadRegional.Id);
        Assert.NotNull(todaviaExiste);
    }
}
