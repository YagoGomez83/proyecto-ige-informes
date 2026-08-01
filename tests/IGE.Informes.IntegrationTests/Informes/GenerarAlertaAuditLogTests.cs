using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Application.Informes.Commands.VincularVehiculoInforme;
using IGE.Informes.Domain.Entities;
using IGE.Informes.Infrastructure.Auditing;
using IGE.Informes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace IGE.Informes.IntegrationTests.Informes;

public class GenerarAlertaAuditLogTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private sealed class FakeCurrentUserService(Guid usuarioId) : ICurrentUserService
    {
        public Guid? UsuarioId { get; } = usuarioId;
        public IReadOnlyCollection<string> Roles { get; } = ["Analista"];
    }

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact]
    public async Task Vincular_un_vehiculo_reincidente_deja_Evidencia_y_Alerta_persistidas_y_ambas_en_AuditLog()
    {
        var usuarioId = Guid.NewGuid();
        var currentUserService = new FakeCurrentUserService(usuarioId);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using (var migrationContext = new AppDbContext(options))
        {
            await migrationContext.Database.MigrateAsync();
        }

        Guid informeAnteriorId;
        Guid informeNuevoId;
        Guid vehiculoId;

        await using (var setupContext = new AppDbContext(options))
        {
            var informeAnterior = Informe.CrearMigrado("1/2026", new DateOnly(2026, 1, 10), Guid.NewGuid(), Guid.NewGuid());
            var informeNuevo = Informe.CrearMigrado("2/2026", new DateOnly(2026, 2, 10), Guid.NewGuid(), Guid.NewGuid());
            var vehiculo = new Vehiculo("Ford", "Fiesta", "Gris", CertezaDominio.Confirmado, AccionARealizar.Identificar, "Comisaría 2°");
            setupContext.Informes.Add(informeAnterior);
            setupContext.Informes.Add(informeNuevo);
            setupContext.Vehiculos.Add(vehiculo);

            var evidenciaAnterior = new Evidencia(1, informeAnterior.Id);
            evidenciaAnterior.VincularVehiculo(vehiculo.Id);
            setupContext.Evidencias.Add(evidenciaAnterior);

            await setupContext.SaveChangesAsync();

            informeAnteriorId = informeAnterior.Id;
            informeNuevoId = informeNuevo.Id;
            vehiculoId = vehiculo.Id;
        }

        var interceptor = new AuditLogInterceptor(currentUserService);
        var optionsConInterceptor = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .AddInterceptors(interceptor)
            .Options;

        await using (var dbContext = new AppDbContext(optionsConInterceptor))
        {
            var handler = new VincularVehiculoInformeCommandHandler(dbContext);
            await handler.Handle(new VincularVehiculoInformeCommand(informeNuevoId, vehiculoId), CancellationToken.None);
        }

        await using (var assertContext = new AppDbContext(options))
        {
            var evidenciaNueva = await assertContext.Evidencias.SingleAsync(e => e.InformeId == informeNuevoId);
            Assert.Contains(vehiculoId, evidenciaNueva.VehiculoIds);

            var alerta = await assertContext.Alertas.SingleAsync(a => a.VehiculoId == vehiculoId);
            Assert.Equal(TipoAlerta.ReincidenciaOtroInforme, alerta.Tipo);
            Assert.Equal(informeNuevoId, alerta.InformeId);
            Assert.Equal(informeAnteriorId, alerta.InformePrevioId);

            var auditoriaEvidencia = await assertContext.AuditLogs
                .Where(a => a.Entidad == nameof(Evidencia) && a.EntidadId == evidenciaNueva.Id)
                .OrderByDescending(a => a.Timestamp)
                .FirstOrDefaultAsync();
            Assert.NotNull(auditoriaEvidencia);
            Assert.Equal("Alta", auditoriaEvidencia.Accion);

            var auditoriaAlerta = await assertContext.AuditLogs
                .Where(a => a.Entidad == nameof(Alerta) && a.EntidadId == alerta.Id)
                .OrderByDescending(a => a.Timestamp)
                .FirstOrDefaultAsync();
            Assert.NotNull(auditoriaAlerta);
            Assert.Equal("Alta", auditoriaAlerta.Accion);
            Assert.Equal(usuarioId, auditoriaAlerta.UsuarioId);
        }
    }
}
