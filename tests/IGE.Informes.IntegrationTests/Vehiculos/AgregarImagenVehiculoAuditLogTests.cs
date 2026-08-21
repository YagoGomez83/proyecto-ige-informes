using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Application.Vehiculos.Commands.AgregarImagenVehiculo;
using IGE.Informes.Domain.Entities;
using IGE.Informes.Infrastructure.Auditing;
using IGE.Informes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace IGE.Informes.IntegrationTests.Vehiculos;

public class AgregarImagenVehiculoAuditLogTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private sealed class FakeCurrentUserService(Guid usuarioId) : ICurrentUserService
    {
        public Guid? UsuarioId { get; } = usuarioId;
        public IReadOnlyCollection<string> Roles { get; } = ["Analista"];
    }

    private sealed class FakeFileStorage : IFileStorage
    {
        public Task<string> SubirAsync(string nombreArchivo, Stream contenido, string tipoMime, CancellationToken cancellationToken = default) =>
            Task.FromResult($"fake/{Guid.NewGuid():N}/{nombreArchivo}");

        public Task<string> ObtenerUrlDescargaAsync(string clave, CancellationToken cancellationToken = default) =>
            Task.FromResult($"https://fake.local/{clave}");

        public Task<byte[]> DescargarAsync(string clave, CancellationToken cancellationToken = default) =>
            Task.FromResult(Array.Empty<byte>());

        public Task EliminarAsync(string clave, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeAntivirusScanner : IAntivirusScanner
    {
        public Task<bool> EstaLimpioAsync(byte[] contenido, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact]
    public async Task Agregar_una_imagen_a_un_vehiculo_ya_persistido_se_inserta_como_Added_y_queda_en_AuditLog()
    {
        // Confirma que VehiculoImagen (entidad normal con FK, no owned
        // collection) no reproduce el bug conocido de OwnsMany con
        // InformeAnalista (ver feedback_efcore_ownsmany_added_como_modified) —
        // agregar una imagen a un Vehículo ya persistido debe insertarse
        // limpio, sin necesitar ningún workaround de ChangeTracker.
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

        await using (var setupContext = new AppDbContext(options))
        {
            var vehiculo = new Vehiculo("Ford", "Fiesta", "Gris", CertezaDominio.Confirmado, AccionARealizar.Identificar, "Comisaría 2°", TipoVehiculo.Auto);
            setupContext.Vehiculos.Add(vehiculo);
            await setupContext.SaveChangesAsync();

            vehiculoId = vehiculo.Id;
        }

        var interceptor = new AuditLogInterceptor(currentUserService);
        var optionsConInterceptor = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .AddInterceptors(interceptor)
            .Options;

        Guid imagenId;

        await using (var dbContext = new AppDbContext(optionsConInterceptor))
        {
            var handler = new AgregarImagenVehiculoCommandHandler(
                dbContext, currentUserService, new FakeFileStorage(), new FakeAntivirusScanner());

            imagenId = await handler.Handle(
                new AgregarImagenVehiculoCommand(vehiculoId, [1, 2, 3], "foto.jpg", "image/jpeg"), CancellationToken.None);
        }

        await using (var assertContext = new AppDbContext(options))
        {
            var imagen = await assertContext.VehiculoImagenes.FindAsync(imagenId);
            Assert.NotNull(imagen);
            Assert.Equal(vehiculoId, imagen!.VehiculoId);

            var registroAuditoria = await assertContext.AuditLogs
                .Where(a => a.Entidad == nameof(VehiculoImagen) && a.EntidadId == imagenId)
                .OrderByDescending(a => a.Timestamp)
                .FirstOrDefaultAsync();

            Assert.NotNull(registroAuditoria);
            Assert.Equal("Alta", registroAuditoria.Accion);
            Assert.Equal(usuarioId, registroAuditoria.UsuarioId);
        }
    }
}
