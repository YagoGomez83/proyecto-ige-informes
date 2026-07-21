using IGE.Informes.Application.Informes.Queries.ListarInformesPorCaso;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Informes;

public class ListarInformesPorCasoQueryHandlerTests
{
    [Fact]
    public async Task Devuelve_solo_los_informes_del_caso_solicitado()
    {
        var dbContext = new TestAppDbContext();
        var casoId = Guid.NewGuid();
        var otroCasoId = Guid.NewGuid();

        dbContext.Informes.Add(new Informe("1/2026", new DateOnly(2026, 7, 20), casoId, Guid.NewGuid(), Guid.NewGuid()));
        dbContext.Informes.Add(new Informe("2/2026", new DateOnly(2026, 7, 21), casoId, Guid.NewGuid(), Guid.NewGuid()));
        dbContext.Informes.Add(new Informe("3/2026", new DateOnly(2026, 7, 21), otroCasoId, Guid.NewGuid(), Guid.NewGuid()));
        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new ListarInformesPorCasoQueryHandler(dbContext, auditLogger);

        var resultado = await handler.Handle(new ListarInformesPorCasoQuery(casoId), CancellationToken.None);

        Assert.Equal(2, resultado.Count);
        Assert.All(resultado, r => Assert.Contains(r.IdRegistro, new[] { "1/2026", "2/2026" }));
        Assert.Single(auditLogger.Registros);
    }
}
