using IGE.Informes.Application.CasosAnalisis.Queries.ListarCasos;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.CasosAnalisis;

public class ListarCasosQueryHandlerTests
{
    [Fact]
    public async Task Devuelve_todos_los_casos_y_registra_el_acceso_de_listado()
    {
        var dbContext = new TestAppDbContext();
        dbContext.CasosAnalisis.Add(new CasoAnalisis(new DateOnly(2026, 7, 21), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));
        dbContext.CasosAnalisis.Add(new CasoAnalisis(new DateOnly(2026, 7, 20), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));
        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new ListarCasosQueryHandler(dbContext, auditLogger);

        var resultado = await handler.Handle(new ListarCasosQuery(), CancellationToken.None);

        Assert.Equal(2, resultado.Count);
        Assert.Single(auditLogger.Registros);
        Assert.Equal(("Listado", nameof(CasoAnalisis), null), auditLogger.Registros[0]);
    }
}
