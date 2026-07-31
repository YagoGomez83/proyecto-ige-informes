using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Application.Personas.Queries.BuscarPersonas;
using IGE.Informes.Domain.Entities;
using IGE.Informes.Infrastructure.Busqueda;
using IGE.Informes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace IGE.Informes.IntegrationTests.Busqueda;

/// <summary>
/// Extensión de HU-05 · Búsqueda de Personas por texto libre. Usa
/// EF.Functions.ILike (case-insensitive) — no traduce en EF Core InMemory,
/// por eso se prueba acá contra un Postgres real.
/// </summary>
public class BuscarPersonasQueryHandlerTests : IAsyncLifetime
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
    public async Task BuscarPersonas_TextoLibreEnMinusculasSinAcentos_EncuentraNombreConAcentos()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using (var migrationContext = new AppDbContext(options))
        {
            await migrationContext.Database.MigrateAsync();
        }

        Guid personaId;
        await using (var setupContext = new AppDbContext(options))
        {
            var persona = new Persona(RolPersona.Sospechoso, nombre: "Juan Pérez", dni: "30111222");
            setupContext.Personas.Add(persona);
            await setupContext.SaveChangesAsync();
            personaId = persona.Id;
        }

        await using var dbContext = new AppDbContext(options);
        var handler = new BuscarPersonasQueryHandler(dbContext, new NullAuditLogger());

        var resultado = await handler.Handle(new BuscarPersonasQuery("perez"), CancellationToken.None);

        Assert.Contains(resultado, p => p.Id == personaId);
        Assert.Equal("30111222", resultado.First(p => p.Id == personaId).Dni);
    }

    [Fact]
    public async Task BuscarPersonas_TextoLibreEsUnDni_EncuentraLaPersona()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using (var migrationContext = new AppDbContext(options))
        {
            await migrationContext.Database.MigrateAsync();
        }

        Guid personaId;
        await using (var setupContext = new AppDbContext(options))
        {
            var persona = new Persona(RolPersona.Sospechoso, nombre: "Juan Pérez", dni: "30111222");
            setupContext.Personas.Add(persona);
            await setupContext.SaveChangesAsync();
            personaId = persona.Id;
        }

        await using var dbContext = new AppDbContext(options);
        var handler = new BuscarPersonasQueryHandler(dbContext, new NullAuditLogger());

        var resultado = await handler.Handle(new BuscarPersonasQuery("30111222"), CancellationToken.None);

        Assert.Contains(resultado, p => p.Id == personaId);
    }

    [Fact]
    public async Task BuscarPersonas_TextoLibreSinCoincidencia_NoDevuelvePersonas()
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
            setupContext.Personas.Add(new Persona(RolPersona.Sospechoso, nombre: "Juan Pérez", dni: "30111222"));
            await setupContext.SaveChangesAsync();
        }

        await using var dbContext = new AppDbContext(options);
        var handler = new BuscarPersonasQueryHandler(dbContext, new NullAuditLogger());

        var resultado = await handler.Handle(new BuscarPersonasQuery("Gómez"), CancellationToken.None);

        Assert.Empty(resultado);
    }
}
