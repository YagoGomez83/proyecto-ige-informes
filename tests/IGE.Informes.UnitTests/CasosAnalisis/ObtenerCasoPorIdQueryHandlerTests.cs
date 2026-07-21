using IGE.Informes.Application.CasosAnalisis.Queries.ObtenerCasoPorId;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.CasosAnalisis;

public class ObtenerCasoPorIdQueryHandlerTests
{
    [Fact]
    public async Task Devuelve_el_caso_y_registra_la_lectura_en_AuditLog()
    {
        var dbContext = new TestAppDbContext();
        var caso = new CasoAnalisis(new DateOnly(2026, 7, 21), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        dbContext.CasosAnalisis.Add(caso);
        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new ObtenerCasoPorIdQueryHandler(dbContext, auditLogger);

        var dto = await handler.Handle(new ObtenerCasoPorIdQuery(caso.Id), CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal(caso.Id, dto.Id);
        Assert.Single(auditLogger.Registros);
        Assert.Equal(("Lectura", nameof(CasoAnalisis), caso.Id), auditLogger.Registros[0]);
    }

    [Fact]
    public async Task Caso_inexistente_devuelve_null_pero_igual_registra_el_intento_de_lectura()
    {
        var dbContext = new TestAppDbContext();
        var auditLogger = new FakeAuditLogger();
        var handler = new ObtenerCasoPorIdQueryHandler(dbContext, auditLogger);

        var idInexistente = Guid.NewGuid();
        var dto = await handler.Handle(new ObtenerCasoPorIdQuery(idInexistente), CancellationToken.None);

        Assert.Null(dto);
        Assert.Single(auditLogger.Registros);
        Assert.Equal(("Lectura", nameof(CasoAnalisis), idInexistente), auditLogger.Registros[0]);
    }
}
