using IGE.Informes.Application.Camaras.Queries.ObtenerCamaraPorId;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Camaras;

public class ObtenerCamaraPorIdQueryHandlerTests
{
    [Fact]
    public async Task Devuelve_la_camara_solicitada()
    {
        var dbContext = new TestAppDbContext();
        var camara = new Camara("SL 18", TipoCamara.Domo, "Av. Illia");
        dbContext.Camaras.Add(camara);
        await dbContext.SaveChangesAsync();

        var handler = new ObtenerCamaraPorIdQueryHandler(dbContext);
        var dto = await handler.Handle(new ObtenerCamaraPorIdQuery(camara.Id), CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal("SL 18", dto.Codigo);
    }

    [Fact]
    public async Task Camara_inexistente_devuelve_null()
    {
        var dbContext = new TestAppDbContext();
        var handler = new ObtenerCamaraPorIdQueryHandler(dbContext);

        var dto = await handler.Handle(new ObtenerCamaraPorIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(dto);
    }
}
