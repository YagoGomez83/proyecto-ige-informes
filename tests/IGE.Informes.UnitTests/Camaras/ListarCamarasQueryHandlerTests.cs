using IGE.Informes.Application.Camaras.Queries.ListarCamaras;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Camaras;

public class ListarCamarasQueryHandlerTests
{
    [Fact]
    public async Task Indica_pendiente_de_ubicacion_cuando_no_tiene_ubicacion()
    {
        var dbContext = new TestAppDbContext();
        dbContext.Camaras.Add(new Camara("SL 18", TipoCamara.Domo, "Av. Illia"));
        dbContext.Camaras.Add(new Camara("JK 51", TipoCamara.Lpr));
        await dbContext.SaveChangesAsync();

        var handler = new ListarCamarasQueryHandler(dbContext);
        var resultado = await handler.Handle(new ListarCamarasQuery(), CancellationToken.None);

        Assert.Equal(2, resultado.Count);
        Assert.Contains(resultado, c => c.Codigo == "SL 18" && !c.PendienteDeUbicacion);
        Assert.Contains(resultado, c => c.Codigo == "JK 51" && c.PendienteDeUbicacion);
    }
}
