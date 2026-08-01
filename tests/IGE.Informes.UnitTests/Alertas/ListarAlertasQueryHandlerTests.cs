using IGE.Informes.Application.Alertas.Queries.ListarAlertas;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Alertas;

public class ListarAlertasQueryHandlerTests
{
    private static Informe CrearInforme(string idRegistro = "290/2026") =>
        Informe.CrearMigrado(idRegistro, new DateOnly(2026, 7, 21), Guid.NewGuid(), Guid.NewGuid());

    [Fact]
    public async Task Lista_las_alertas_con_el_resumen_del_vehiculo_y_del_informe()
    {
        var dbContext = new TestAppDbContext();
        var informe = CrearInforme();
        var vehiculo = new Vehiculo("Ford", "Fiesta", "Gris", CertezaDominio.Confirmado, AccionARealizar.Identificar, "Comisaría 2°", dominio: "ABC123");
        dbContext.Informes.Add(informe);
        dbContext.Vehiculos.Add(vehiculo);
        dbContext.Alertas.Add(Alerta.PorCargaHuerfana(vehiculo.Id, personaId: null, informe.Id));
        await dbContext.SaveChangesAsync();

        var handler = new ListarAlertasQueryHandler(dbContext, new FakeAuditLogger());

        var resultado = await handler.Handle(new ListarAlertasQuery(), CancellationToken.None);

        var dto = Assert.Single(resultado.Items);
        Assert.Equal(TipoAlerta.CargaHuerfana, dto.Tipo);
        Assert.Contains("ABC123", dto.VehiculoResumen);
        Assert.Equal("290/2026", dto.InformeIdRegistro);
        Assert.False(dto.Atendida);
    }

    [Fact]
    public async Task Filtro_SoloNoAtendidas_excluye_las_ya_atendidas()
    {
        var dbContext = new TestAppDbContext();
        var informe = CrearInforme();
        dbContext.Informes.Add(informe);

        var alertaAtendida = Alerta.PorCargaHuerfana(Guid.NewGuid(), personaId: null, informe.Id);
        alertaAtendida.MarcarAtendida(Guid.NewGuid());
        var alertaPendiente = Alerta.PorCargaHuerfana(Guid.NewGuid(), personaId: null, informe.Id);

        dbContext.Alertas.Add(alertaAtendida);
        dbContext.Alertas.Add(alertaPendiente);
        await dbContext.SaveChangesAsync();

        var handler = new ListarAlertasQueryHandler(dbContext, new FakeAuditLogger());

        var resultado = await handler.Handle(new ListarAlertasQuery(SoloNoAtendidas: true), CancellationToken.None);

        var dto = Assert.Single(resultado.Items);
        Assert.Equal(alertaPendiente.Id, dto.Id);
    }

    [Fact]
    public async Task Sin_filtro_devuelve_atendidas_y_pendientes()
    {
        var dbContext = new TestAppDbContext();
        var informe = CrearInforme();
        dbContext.Informes.Add(informe);

        var alertaAtendida = Alerta.PorCargaHuerfana(Guid.NewGuid(), personaId: null, informe.Id);
        alertaAtendida.MarcarAtendida(Guid.NewGuid());
        var alertaPendiente = Alerta.PorCargaHuerfana(Guid.NewGuid(), personaId: null, informe.Id);

        dbContext.Alertas.Add(alertaAtendida);
        dbContext.Alertas.Add(alertaPendiente);
        await dbContext.SaveChangesAsync();

        var handler = new ListarAlertasQueryHandler(dbContext, new FakeAuditLogger());

        var resultado = await handler.Handle(new ListarAlertasQuery(), CancellationToken.None);

        Assert.Equal(2, resultado.Items.Count);
    }

    [Fact]
    public async Task Ordena_por_fecha_de_generacion_descendente()
    {
        var dbContext = new TestAppDbContext();
        var informe = CrearInforme();
        dbContext.Informes.Add(informe);

        var alertaAntigua = Alerta.PorCargaHuerfana(Guid.NewGuid(), personaId: null, informe.Id);
        await Task.Delay(10);
        var alertaReciente = Alerta.PorCargaHuerfana(Guid.NewGuid(), personaId: null, informe.Id);

        dbContext.Alertas.Add(alertaAntigua);
        dbContext.Alertas.Add(alertaReciente);
        await dbContext.SaveChangesAsync();

        var handler = new ListarAlertasQueryHandler(dbContext, new FakeAuditLogger());

        var resultado = (await handler.Handle(new ListarAlertasQuery(), CancellationToken.None)).Items.ToList();

        Assert.Equal(alertaReciente.Id, resultado[0].Id);
        Assert.Equal(alertaAntigua.Id, resultado[1].Id);
    }
}
