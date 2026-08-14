using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Application.Vehiculos.Queries.BuscarVehiculos;
using IGE.Informes.Domain.Entities;
using IGE.Informes.Infrastructure.Busqueda;
using IGE.Informes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace IGE.Informes.IntegrationTests.Busqueda;

/// <summary>
/// Extensión de HU-05 · Búsqueda de Vehículos por texto libre. Usa
/// EF.Functions.ILike (case-insensitive) — no traduce en EF Core InMemory,
/// por eso se prueba acá contra un Postgres real, mismo patrón que
/// BuscarInformesTextoLibreTests.
/// </summary>
public class BuscarVehiculosQueryHandlerTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private sealed class NullAuditLogger : IAuditLogger
    {
        public Task RegistrarAccesoAsync(string accion, string entidad, Guid? entidadId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact]
    public async Task BuscarVehiculos_TextoLibreEnMinusculas_EncuentraDominioEnMayusculas()
    {
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
            var vehiculo = new Vehiculo("Volkswagen", "Gol", "Gris", CertezaDominio.Confirmado, AccionARealizar.Identificar, "Comisaría", TipoVehiculo.Auto, "IAK 796");
            setupContext.Vehiculos.Add(vehiculo);
            await setupContext.SaveChangesAsync();
            vehiculoId = vehiculo.Id;
        }

        await using var dbContext = new AppDbContext(options);
        var handler = new BuscarVehiculosQueryHandler(dbContext, new NullAuditLogger());

        var resultado = await handler.Handle(new BuscarVehiculosQuery("iak796"), CancellationToken.None);

        Assert.Contains(resultado, v => v.Id == vehiculoId);
    }

    [Fact]
    public async Task BuscarVehiculos_TextoLibreSinEspaciosNiGuiones_EncuentraDominioConFormatoDistinto()
    {
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
            var vehiculo = new Vehiculo("Volkswagen", "Gol", "Gris", CertezaDominio.Confirmado, AccionARealizar.Identificar, "Comisaría", TipoVehiculo.Auto, "IAK-796");
            setupContext.Vehiculos.Add(vehiculo);
            await setupContext.SaveChangesAsync();
            vehiculoId = vehiculo.Id;
        }

        await using var dbContext = new AppDbContext(options);
        var handler = new BuscarVehiculosQueryHandler(dbContext, new NullAuditLogger());

        var resultado = await handler.Handle(new BuscarVehiculosQuery("IAK796"), CancellationToken.None);

        Assert.Contains(resultado, v => v.Id == vehiculoId);
    }

    [Fact]
    public async Task BuscarVehiculos_TextoLibreSinCoincidencia_NoDevuelveVehiculos()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using (var migrationContext = new AppDbContext(options))
        {
            await migrationContext.Database.MigrateAsync();
        }

        await using (var setupContext = new AppDbContext(options))
        {
            setupContext.Vehiculos.Add(new Vehiculo("Volkswagen", "Gol", "Gris", CertezaDominio.Confirmado, AccionARealizar.Identificar, "Comisaría", TipoVehiculo.Auto, "IAK796"));
            await setupContext.SaveChangesAsync();
        }

        await using var dbContext = new AppDbContext(options);
        var handler = new BuscarVehiculosQueryHandler(dbContext, new NullAuditLogger());

        var resultado = await handler.Handle(new BuscarVehiculosQuery("ZZZ999"), CancellationToken.None);

        Assert.Empty(resultado);
    }
}
