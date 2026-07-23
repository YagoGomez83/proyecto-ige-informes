using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Dependencias.Commands.AsignarBarrioADependencia;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Dependencias;

public class AsignarBarrioADependenciaCommandHandlerTests
{
    [Fact]
    public async Task Asigna_el_barrio_a_la_dependencia()
    {
        var dbContext = new TestAppDbContext();
        var dependencia = new Dependencia("Comisaría 2°", TipoDependencia.Comisaria);
        var barrio = new Barrio("Barrio Norte");
        dbContext.Dependencias.Add(dependencia);
        dbContext.Barrios.Add(barrio);
        await dbContext.SaveChangesAsync();

        var handler = new AsignarBarrioADependenciaCommandHandler(dbContext);

        await handler.Handle(new AsignarBarrioADependenciaCommand(dependencia.Id, barrio.Id), CancellationToken.None);

        var actualizada = await dbContext.Dependencias.FindAsync(dependencia.Id);
        Assert.Contains(barrio.Id, actualizada!.BarrioIds);
    }

    [Fact]
    public async Task Rechaza_una_dependencia_inexistente()
    {
        var dbContext = new TestAppDbContext();
        var barrio = new Barrio("Barrio Norte");
        dbContext.Barrios.Add(barrio);
        await dbContext.SaveChangesAsync();

        var handler = new AsignarBarrioADependenciaCommandHandler(dbContext);

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            new AsignarBarrioADependenciaCommand(Guid.NewGuid(), barrio.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Rechaza_un_barrio_inexistente()
    {
        var dbContext = new TestAppDbContext();
        var dependencia = new Dependencia("Comisaría 2°", TipoDependencia.Comisaria);
        dbContext.Dependencias.Add(dependencia);
        await dbContext.SaveChangesAsync();

        var handler = new AsignarBarrioADependenciaCommandHandler(dbContext);

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            new AsignarBarrioADependenciaCommand(dependencia.Id, Guid.NewGuid()), CancellationToken.None));
    }
}
