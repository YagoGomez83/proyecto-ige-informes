using IGE.Informes.Application.Informes.Queries.ObtenerInformePorId;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Informes;

public class ObtenerInformePorIdQueryHandlerTests
{
    [Fact]
    public async Task Devuelve_el_informe_y_registra_la_lectura_en_AuditLog()
    {
        var dbContext = new TestAppDbContext();
        var informe = new Informe("290/2026", new DateOnly(2026, 7, 21), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        dbContext.Informes.Add(informe);
        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new ObtenerInformePorIdQueryHandler(dbContext, auditLogger);

        var dto = await handler.Handle(new ObtenerInformePorIdQuery(informe.Id), CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal("290/2026", dto.IdRegistro);
        Assert.Single(auditLogger.Registros);
        Assert.Equal(("Lectura", nameof(Informe), informe.Id), auditLogger.Registros[0]);
    }

    [Fact]
    public async Task Informe_inexistente_devuelve_null_pero_igual_registra_el_intento_de_lectura()
    {
        var dbContext = new TestAppDbContext();
        var auditLogger = new FakeAuditLogger();
        var handler = new ObtenerInformePorIdQueryHandler(dbContext, auditLogger);

        var idInexistente = Guid.NewGuid();
        var dto = await handler.Handle(new ObtenerInformePorIdQuery(idInexistente), CancellationToken.None);

        Assert.Null(dto);
        Assert.Single(auditLogger.Registros);
    }
}
