using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Dependencias.Commands.AsignarUnidadRegionalADependencia;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Dependencias;

public class AsignarUnidadRegionalADependenciaCommandHandlerTests
{
    [Fact]
    public async Task Asigna_la_unidad_regional_a_la_comisaria()
    {
        var dbContext = new TestAppDbContext();
        var comisaria = new Dependencia("Comisaría 2°", TipoDependencia.Comisaria);
        var unidadRegional = new Dependencia("UR 1", TipoDependencia.UnidadRegional);
        dbContext.Dependencias.Add(comisaria);
        dbContext.Dependencias.Add(unidadRegional);
        await dbContext.SaveChangesAsync();

        var handler = new AsignarUnidadRegionalADependenciaCommandHandler(dbContext);

        await handler.Handle(
            new AsignarUnidadRegionalADependenciaCommand(comisaria.Id, unidadRegional.Id), CancellationToken.None);

        var actualizada = await dbContext.Dependencias.FindAsync(comisaria.Id);
        Assert.Equal(unidadRegional.Id, actualizada!.UnidadRegionalId);
    }

    [Fact]
    public async Task Rechaza_una_dependencia_inexistente()
    {
        var dbContext = new TestAppDbContext();
        var unidadRegional = new Dependencia("UR 1", TipoDependencia.UnidadRegional);
        dbContext.Dependencias.Add(unidadRegional);
        await dbContext.SaveChangesAsync();

        var handler = new AsignarUnidadRegionalADependenciaCommandHandler(dbContext);

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            new AsignarUnidadRegionalADependenciaCommand(Guid.NewGuid(), unidadRegional.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Rechaza_una_unidad_regional_inexistente()
    {
        var dbContext = new TestAppDbContext();
        var comisaria = new Dependencia("Comisaría 2°", TipoDependencia.Comisaria);
        dbContext.Dependencias.Add(comisaria);
        await dbContext.SaveChangesAsync();

        var handler = new AsignarUnidadRegionalADependenciaCommandHandler(dbContext);

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            new AsignarUnidadRegionalADependenciaCommand(comisaria.Id, Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Rechaza_una_dependencia_referenciada_que_no_es_unidad_regional()
    {
        var dbContext = new TestAppDbContext();
        var comisaria = new Dependencia("Comisaría 2°", TipoDependencia.Comisaria);
        var fiscalia = new Dependencia("Fiscalía N°3", TipoDependencia.Fiscalia);
        dbContext.Dependencias.Add(comisaria);
        dbContext.Dependencias.Add(fiscalia);
        await dbContext.SaveChangesAsync();

        var handler = new AsignarUnidadRegionalADependenciaCommandHandler(dbContext);

        await Assert.ThrowsAsync<ReglaDeNegocioVioladaException>(() => handler.Handle(
            new AsignarUnidadRegionalADependenciaCommand(comisaria.Id, fiscalia.Id), CancellationToken.None));
    }
}
