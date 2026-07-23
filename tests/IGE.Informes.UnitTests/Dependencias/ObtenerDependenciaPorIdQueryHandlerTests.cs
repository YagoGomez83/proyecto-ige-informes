using IGE.Informes.Application.Dependencias.Queries.ObtenerDependenciaPorId;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Dependencias;

public class ObtenerDependenciaPorIdQueryHandlerTests
{
    [Fact]
    public async Task Devuelve_la_dependencia_con_sus_barrios()
    {
        var dbContext = new TestAppDbContext();
        var dependencia = new Dependencia("Comisaría 2°", TipoDependencia.Comisaria);
        var barrioId = Guid.NewGuid();
        dependencia.AsignarBarrio(barrioId);
        dbContext.Dependencias.Add(dependencia);
        await dbContext.SaveChangesAsync();

        var handler = new ObtenerDependenciaPorIdQueryHandler(dbContext);

        var resultado = await handler.Handle(new ObtenerDependenciaPorIdQuery(dependencia.Id), CancellationToken.None);

        Assert.NotNull(resultado);
        Assert.Equal("Comisaría 2°", resultado!.Nombre);
        Assert.Contains(barrioId, resultado.BarrioIds);
    }

    [Fact]
    public async Task Devuelve_null_si_no_existe()
    {
        var dbContext = new TestAppDbContext();
        var handler = new ObtenerDependenciaPorIdQueryHandler(dbContext);

        var resultado = await handler.Handle(new ObtenerDependenciaPorIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(resultado);
    }
}
