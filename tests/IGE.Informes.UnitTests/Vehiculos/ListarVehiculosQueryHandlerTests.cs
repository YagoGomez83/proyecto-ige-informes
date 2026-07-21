using IGE.Informes.Application.Vehiculos.Queries.ListarVehiculos;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Vehiculos;

public class ListarVehiculosQueryHandlerTests
{
    [Fact]
    public async Task Devuelve_todos_los_vehiculos_y_registra_el_acceso_de_listado()
    {
        var dbContext = new TestAppDbContext();
        dbContext.Vehiculos.Add(new Vehiculo("Volkswagen", "Gol", "Gris", CertezaDominio.Incierto, AccionARealizar.Detener, "Comisaría 2°"));
        dbContext.Vehiculos.Add(new Vehiculo("Ford", "Fiesta", "Rojo", CertezaDominio.Confirmado, AccionARealizar.Identificar, "Fiscalía N°3"));
        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new ListarVehiculosQueryHandler(dbContext, auditLogger);

        var resultado = await handler.Handle(new ListarVehiculosQuery(), CancellationToken.None);

        Assert.Equal(2, resultado.Count);
        Assert.Single(auditLogger.Registros);
        Assert.Equal(("Listado", nameof(Vehiculo), null), auditLogger.Registros[0]);
    }
}
