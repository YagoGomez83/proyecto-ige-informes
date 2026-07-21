using IGE.Informes.Application.Vehiculos.Queries.ObtenerVehiculoPorId;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Vehiculos;

public class ObtenerVehiculoPorIdQueryHandlerTests
{
    [Fact]
    public async Task Devuelve_el_vehiculo_y_registra_la_lectura_en_AuditLog()
    {
        var dbContext = new TestAppDbContext();
        var vehiculo = new Vehiculo("Volkswagen", "Gol", "Gris", CertezaDominio.Incierto, AccionARealizar.Detener, "Comisaría 2°");
        dbContext.Vehiculos.Add(vehiculo);
        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new ObtenerVehiculoPorIdQueryHandler(dbContext, auditLogger);

        var dto = await handler.Handle(new ObtenerVehiculoPorIdQuery(vehiculo.Id), CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal("Volkswagen", dto.Marca);
        Assert.Single(auditLogger.Registros);
        Assert.Equal(("Lectura", nameof(Vehiculo), vehiculo.Id), auditLogger.Registros[0]);
    }

    [Fact]
    public async Task Vehiculo_inexistente_devuelve_null_pero_igual_registra_el_intento_de_lectura()
    {
        var dbContext = new TestAppDbContext();
        var auditLogger = new FakeAuditLogger();
        var handler = new ObtenerVehiculoPorIdQueryHandler(dbContext, auditLogger);

        var dto = await handler.Handle(new ObtenerVehiculoPorIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(dto);
        Assert.Single(auditLogger.Registros);
    }
}
