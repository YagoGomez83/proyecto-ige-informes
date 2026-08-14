using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Application.Informes.Commands.VincularVehiculoInforme;
using IGE.Informes.Domain.Entities;
using IGE.Informes.Infrastructure.Auditing;
using IGE.Informes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace IGE.Informes.IntegrationTests.Informes;

public class VincularVehiculoInformeAuditLogTests : IAsyncLifetime
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
    public async Task Vincular_un_vehiculo_crea_una_Evidencia_manual_y_queda_en_AuditLog()
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

        Guid informeId;
        Guid vehiculoId;

        await using (var setupContext = new AppDbContext(options))
        {
            var informe = Informe.CrearMigrado("290/2026", new DateOnly(2026, 7, 21), Guid.NewGuid(), Guid.NewGuid());
            var vehiculo = new Vehiculo("Ford", "Fiesta", "Gris", CertezaDominio.Confirmado, AccionARealizar.Identificar, "Comisaría 2°", TipoVehiculo.Auto);
            setupContext.Informes.Add(informe);
            setupContext.Vehiculos.Add(vehiculo);
            await setupContext.SaveChangesAsync();

            informeId = informe.Id;
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
            await handler.Handle(new VincularVehiculoInformeCommand(informeId, vehiculoId), CancellationToken.None);
        }

        await using (var assertContext = new AppDbContext(options))
        {
            var evidencia = await assertContext.Evidencias.SingleAsync(e => e.InformeId == informeId);
            Assert.Contains(vehiculoId, evidencia.VehiculoIds);
            Assert.Null(evidencia.ImagenPath);
            Assert.Null(evidencia.CamaraId);

            var registroAuditoria = await assertContext.AuditLogs
                .Where(a => a.Entidad == nameof(Evidencia) && a.EntidadId == evidencia.Id)
                .OrderByDescending(a => a.Timestamp)
                .FirstOrDefaultAsync();

            Assert.NotNull(registroAuditoria);
            Assert.Equal("Alta", registroAuditoria.Accion);
            Assert.Equal(usuarioId, registroAuditoria.UsuarioId);
        }
    }
}
