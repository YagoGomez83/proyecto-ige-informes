using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Application.Vehiculos.Commands.AsignarCategoriaAlerta;
using IGE.Informes.Domain.Entities;
using IGE.Informes.Infrastructure.Auditing;
using IGE.Informes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace IGE.Informes.IntegrationTests.Vehiculos;

public class AsignarCategoriaAlertaAuditLogTests : IAsyncLifetime
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
    public async Task Modificar_solo_CategoriasAlertaIds_dispara_EntityState_Modified_y_queda_en_AuditLog()
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

        Guid vehiculoId;
        Guid categoriaAlertaId;

        await using (var setupContext = new AppDbContext(options))
        {
            var vehiculo = new Vehiculo("Volkswagen", "Gol", "Gris", CertezaDominio.Incierto, AccionARealizar.Detener, "Comisaría 2°", TipoVehiculo.Auto);
            var categoria = new CategoriaAlerta("Robado");
            setupContext.Vehiculos.Add(vehiculo);
            setupContext.CategoriasAlerta.Add(categoria);
            await setupContext.SaveChangesAsync();

            vehiculoId = vehiculo.Id;
            categoriaAlertaId = categoria.Id;
        }

        var interceptor = new AuditLogInterceptor(currentUserService);
        var optionsConInterceptor = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .AddInterceptors(interceptor)
            .Options;

        await using (var dbContext = new AppDbContext(optionsConInterceptor))
        {
            var handler = new AsignarCategoriaAlertaCommandHandler(dbContext);

            await handler.Handle(new AsignarCategoriaAlertaCommand(vehiculoId, categoriaAlertaId), CancellationToken.None);
        }

        await using (var assertContext = new AppDbContext(options))
        {
            var vehiculoActualizado = await assertContext.Vehiculos.FindAsync(vehiculoId);
            Assert.Contains(categoriaAlertaId, vehiculoActualizado!.CategoriasAlertaIds);

            var registroAuditoria = await assertContext.AuditLogs
                .Where(a => a.Entidad == nameof(Vehiculo) && a.EntidadId == vehiculoId)
                .OrderByDescending(a => a.Timestamp)
                .FirstOrDefaultAsync();

            Assert.NotNull(registroAuditoria);
            Assert.Equal("Modificacion", registroAuditoria.Accion);
            Assert.Equal(usuarioId, registroAuditoria.UsuarioId);
        }
    }
}
