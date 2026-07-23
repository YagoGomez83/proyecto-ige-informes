using IGE.Informes.Application.Camaras.Commands.AsignarDependenciaACamara;
using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Camaras;

public class AsignarDependenciaACamaraCommandHandlerTests
{
    [Fact]
    public async Task Asigna_la_dependencia_a_la_camara()
    {
        var dbContext = new TestAppDbContext();
        var camara = new Camara("LP 217", TipoCamara.Lpr);
        var dependencia = new Dependencia("Comisaría 2°", TipoDependencia.Comisaria);
        dbContext.Camaras.Add(camara);
        dbContext.Dependencias.Add(dependencia);
        await dbContext.SaveChangesAsync();

        var handler = new AsignarDependenciaACamaraCommandHandler(dbContext);

        await handler.Handle(new AsignarDependenciaACamaraCommand(camara.Id, dependencia.Id), CancellationToken.None);

        var actualizada = await dbContext.Camaras.FindAsync(camara.Id);
        Assert.Equal(dependencia.Id, actualizada!.DependenciaId);
    }

    [Fact]
    public async Task Rechaza_una_camara_inexistente()
    {
        var dbContext = new TestAppDbContext();
        var dependencia = new Dependencia("Comisaría 2°", TipoDependencia.Comisaria);
        dbContext.Dependencias.Add(dependencia);
        await dbContext.SaveChangesAsync();

        var handler = new AsignarDependenciaACamaraCommandHandler(dbContext);

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            new AsignarDependenciaACamaraCommand(Guid.NewGuid(), dependencia.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Rechaza_una_dependencia_inexistente()
    {
        var dbContext = new TestAppDbContext();
        var camara = new Camara("LP 217", TipoCamara.Lpr);
        dbContext.Camaras.Add(camara);
        await dbContext.SaveChangesAsync();

        var handler = new AsignarDependenciaACamaraCommandHandler(dbContext);

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            new AsignarDependenciaACamaraCommand(camara.Id, Guid.NewGuid()), CancellationToken.None));
    }
}
