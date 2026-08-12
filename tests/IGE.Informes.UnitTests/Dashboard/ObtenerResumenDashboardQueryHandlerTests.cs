using IGE.Informes.Application.Dashboard.Queries.ObtenerResumenDashboard;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Dashboard;

public class ObtenerResumenDashboardQueryHandlerTests
{
    private static CasoAnalisis CrearCaso(EstadoCaso estado)
    {
        var caso = new CasoAnalisis(new DateOnly(2026, 1, 10), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        if (estado == EstadoCaso.EnRevision)
        {
            caso.MarcarEnRevision();
        }
        else if (estado == EstadoCaso.Cerrado)
        {
            caso.CerrarConResultado(ResultadoCaso.Positivo);
        }

        return caso;
    }

    private static Vehiculo CrearVehiculo(bool identificado)
    {
        var vehiculo = new Vehiculo("Ford", "Fiesta", "Gris", CertezaDominio.Incierto, AccionARealizar.Identificar, "Comisaría", "ABC123");

        if (identificado)
        {
            vehiculo.MarcarIdentificado();
        }

        return vehiculo;
    }

    private static Informe CrearInforme(bool publicado)
    {
        var informe = Informe.CrearMigrado("1/2026", new DateOnly(2026, 1, 10), Guid.NewGuid(), Guid.NewGuid(), causaId: Guid.NewGuid());

        if (publicado)
        {
            informe.AgregarFirmante(Guid.NewGuid());
            informe.Publicar();
        }

        return informe;
    }

    [Fact]
    public async Task ObtenerResumenDashboard_CuentaCasosAbiertosComoPendienteMasEnRevision()
    {
        var dbContext = new TestAppDbContext();
        dbContext.CasosAnalisis.Add(CrearCaso(EstadoCaso.Pendiente));
        dbContext.CasosAnalisis.Add(CrearCaso(EstadoCaso.EnRevision));
        dbContext.CasosAnalisis.Add(CrearCaso(EstadoCaso.Cerrado));
        await dbContext.SaveChangesAsync();

        var handler = new ObtenerResumenDashboardQueryHandler(dbContext, new FakeAuditLogger());

        var resultado = await handler.Handle(new ObtenerResumenDashboardQuery(), CancellationToken.None);

        Assert.Equal(2, resultado.CasosAbiertos);
    }

    [Fact]
    public async Task ObtenerResumenDashboard_CuentaSoloVehiculosConEstadoVigente()
    {
        var dbContext = new TestAppDbContext();
        dbContext.Vehiculos.Add(CrearVehiculo(identificado: false));
        dbContext.Vehiculos.Add(CrearVehiculo(identificado: false));
        dbContext.Vehiculos.Add(CrearVehiculo(identificado: true));
        await dbContext.SaveChangesAsync();

        var handler = new ObtenerResumenDashboardQueryHandler(dbContext, new FakeAuditLogger());

        var resultado = await handler.Handle(new ObtenerResumenDashboardQuery(), CancellationToken.None);

        Assert.Equal(2, resultado.VehiculosConAlertaVigente);
    }

    [Fact]
    public async Task ObtenerResumenDashboard_VehiculoVigenteSinFechaBaja_DebeContarComoAlertaVigente()
    {
        var dbContext = new TestAppDbContext();
        dbContext.Vehiculos.Add(CrearVehiculo(identificado: false));
        await dbContext.SaveChangesAsync();

        var handler = new ObtenerResumenDashboardQueryHandler(dbContext, new FakeAuditLogger());

        var resultado = await handler.Handle(new ObtenerResumenDashboardQuery(), CancellationToken.None);

        Assert.Equal(1, resultado.VehiculosConAlertaVigente);
    }

    [Fact]
    public async Task ObtenerResumenDashboard_VehiculoVigenteConFechaBaja_NoDebeContarComoAlertaVigente()
    {
        var dbContext = new TestAppDbContext();
        var vehiculoDadoDeBaja = CrearVehiculo(identificado: false);
        vehiculoDadoDeBaja.DarDeBaja(new DateOnly(2026, 8, 1));
        dbContext.Vehiculos.Add(vehiculoDadoDeBaja);
        await dbContext.SaveChangesAsync();

        var handler = new ObtenerResumenDashboardQueryHandler(dbContext, new FakeAuditLogger());

        var resultado = await handler.Handle(new ObtenerResumenDashboardQuery(), CancellationToken.None);

        Assert.Equal(0, resultado.VehiculosConAlertaVigente);
    }

    [Fact]
    public async Task ObtenerResumenDashboard_VehiculoIdentificado_NoDebeContarComoAlertaVigente()
    {
        var dbContext = new TestAppDbContext();
        dbContext.Vehiculos.Add(CrearVehiculo(identificado: true));
        await dbContext.SaveChangesAsync();

        var handler = new ObtenerResumenDashboardQueryHandler(dbContext, new FakeAuditLogger());

        var resultado = await handler.Handle(new ObtenerResumenDashboardQuery(), CancellationToken.None);

        Assert.Equal(0, resultado.VehiculosConAlertaVigente);
    }

    [Fact]
    public async Task ObtenerResumenDashboard_CuentaInformesEnBorradorYPublicadosPorSeparado()
    {
        var dbContext = new TestAppDbContext();
        dbContext.Informes.Add(CrearInforme(publicado: false));
        dbContext.Informes.Add(CrearInforme(publicado: true));
        dbContext.Informes.Add(CrearInforme(publicado: true));
        await dbContext.SaveChangesAsync();

        var handler = new ObtenerResumenDashboardQueryHandler(dbContext, new FakeAuditLogger());

        var resultado = await handler.Handle(new ObtenerResumenDashboardQuery(), CancellationToken.None);

        Assert.Equal(1, resultado.InformesEnBorrador);
        Assert.Equal(2, resultado.InformesPublicados);
    }

    [Fact]
    public async Task ObtenerResumenDashboard_RegistraElAccesoEnAuditLog()
    {
        var dbContext = new TestAppDbContext();
        var auditLogger = new FakeAuditLogger();
        var handler = new ObtenerResumenDashboardQueryHandler(dbContext, auditLogger);

        await handler.Handle(new ObtenerResumenDashboardQuery(), CancellationToken.None);

        Assert.Single(auditLogger.Registros);
        Assert.Equal("Dashboard", auditLogger.Registros[0].Accion);
        Assert.Equal(nameof(CasoAnalisis), auditLogger.Registros[0].Entidad);
        Assert.Null(auditLogger.Registros[0].EntidadId);
    }
}
