using IGE.Informes.Application.Barrios.Queries.ListarBarrios;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Barrios;

public class ListarBarriosQueryHandlerTests
{
    [Fact]
    public async Task Devuelve_los_barrios_ordenados_por_nombre()
    {
        var dbContext = new TestAppDbContext();
        dbContext.Barrios.Add(new Barrio("Barrio Sur"));
        dbContext.Barrios.Add(new Barrio("Barrio Norte"));
        await dbContext.SaveChangesAsync();

        var handler = new ListarBarriosQueryHandler(dbContext);

        var resultado = (await handler.Handle(new ListarBarriosQuery(), CancellationToken.None)).ToList();

        Assert.Equal(2, resultado.Count);
        Assert.Equal("Barrio Norte", resultado[0].Nombre);
        Assert.Equal("Barrio Sur", resultado[1].Nombre);
    }
}
