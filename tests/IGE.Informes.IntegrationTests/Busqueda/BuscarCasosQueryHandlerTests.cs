using IGE.Informes.Application.CasosAnalisis.Queries.BuscarCasos;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using IGE.Informes.Infrastructure.Busqueda;
using IGE.Informes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace IGE.Informes.IntegrationTests.Busqueda;

/// <summary>
/// Extensión de HU-05 · Búsqueda de Casos de Análisis por texto libre. Usa
/// EF.Functions.ILike (case-insensitive) — no traduce en EF Core InMemory,
/// por eso se prueba acá contra un Postgres real.
/// </summary>
public class BuscarCasosQueryHandlerTests : IAsyncLifetime
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
    public async Task BuscarCasos_TextoLibreEnMinusculas_EncuentraObservacionesConMayusculas()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using (var migrationContext = new AppDbContext(options))
        {
            await migrationContext.Database.MigrateAsync();
        }

        Guid casoId;
        await using (var setupContext = new AppDbContext(options))
        {
            var caso = new CasoAnalisis(new DateOnly(2026, 7, 20), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), observaciones: "Sospecha de Narcotráfico en la zona norte");
            setupContext.CasosAnalisis.Add(caso);
            await setupContext.SaveChangesAsync();
            casoId = caso.Id;
        }

        await using var dbContext = new AppDbContext(options);
        var handler = new BuscarCasosQueryHandler(dbContext, new NullAuditLogger());

        var resultado = await handler.Handle(new BuscarCasosQuery("narcotráfico"), CancellationToken.None);

        Assert.Contains(resultado, c => c.Id == casoId);
    }

    [Fact]
    public async Task BuscarCasos_TextoLibreEnVehiculoInvolucradoTexto_EncuentraElCaso()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using (var migrationContext = new AppDbContext(options))
        {
            await migrationContext.Database.MigrateAsync();
        }

        Guid casoId;
        await using (var setupContext = new AppDbContext(options))
        {
            var caso = new CasoAnalisis(new DateOnly(2026, 7, 20), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            caso.VincularVehiculoTexto("Ford Fiesta gris, sin patente visible");
            setupContext.CasosAnalisis.Add(caso);
            await setupContext.SaveChangesAsync();
            casoId = caso.Id;
        }

        await using var dbContext = new AppDbContext(options);
        var handler = new BuscarCasosQueryHandler(dbContext, new NullAuditLogger());

        var resultado = await handler.Handle(new BuscarCasosQuery("fiesta"), CancellationToken.None);

        Assert.Contains(resultado, c => c.Id == casoId);
    }

    [Fact]
    public async Task BuscarCasos_TextoLibreSinCoincidencia_NoDevuelveCasos()
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
            setupContext.CasosAnalisis.Add(new CasoAnalisis(new DateOnly(2026, 7, 20), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), observaciones: "Robo de bicicleta"));
            await setupContext.SaveChangesAsync();
        }

        await using var dbContext = new AppDbContext(options);
        var handler = new BuscarCasosQueryHandler(dbContext, new NullAuditLogger());

        var resultado = await handler.Handle(new BuscarCasosQuery("narcotráfico"), CancellationToken.None);

        Assert.Empty(resultado);
    }
}
