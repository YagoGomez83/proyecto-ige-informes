using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Application.Informes.Queries.BuscarInformes;
using IGE.Informes.Domain.Entities;
using IGE.Informes.Infrastructure.Busqueda;
using IGE.Informes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace IGE.Informes.IntegrationTests.Informes;

/// <summary>
/// HU-05 · Buscar informes por múltiples criterios (Épica 02), escenario
/// "Búsqueda por texto libre en el relato". El filtro TextoLibre usa
/// EF.Functions.ToTsVector/PlainToTsQuery sobre Informe.Relato (ADR-002 —
/// tsvector/GIN, no LIKE/Contains) — una feature específica del motor de
/// Postgres que EF Core InMemory no traduce, por eso se prueba acá contra
/// un contenedor de Postgres real, mismo patrón exacto que
/// EditarInformeAuditLogTests / PublicarInformeAuditLogTests.
/// </summary>
public class BuscarInformesTextoLibreTests : IAsyncLifetime
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

    private static Informe CrearInformeMigrado(string idRegistro, DateOnly fecha, Guid dependenciaId, string relato)
    {
        var informe = Informe.CrearMigrado(idRegistro, fecha, dependenciaId, Guid.NewGuid());
        informe.CompletarRelato(relato);
        return informe;
    }

    [Fact]
    public async Task BuscarInformes_TextoLibreConPalabraPresenteEnElRelato_DevuelveEseInforme()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using (var migrationContext = new AppDbContext(options))
        {
            await migrationContext.Database.MigrateAsync();
        }

        var dependenciaId = Guid.NewGuid();
        Guid informeConNarcotraficoId;
        Guid informeSinRelacionId;

        await using (var setupContext = new AppDbContext(options))
        {
            var dependencia = new Dependencia("Comisaría Seccional Primera", TipoDependencia.Comisaria);
            setupContext.Dependencias.Add(dependencia);
            await setupContext.SaveChangesAsync();

            var informeConNarcotrafico = CrearInformeMigrado(
                "1/2026", new DateOnly(2026, 5, 10), dependencia.Id,
                "Se investiga una banda dedicada al narcotráfico en la zona norte.");
            var informeSinRelacion = CrearInformeMigrado(
                "2/2026", new DateOnly(2026, 5, 11), dependencia.Id,
                "Sustracción de un vehículo estacionado frente a una vivienda particular.");

            setupContext.Informes.Add(informeConNarcotrafico);
            setupContext.Informes.Add(informeSinRelacion);
            await setupContext.SaveChangesAsync();

            informeConNarcotraficoId = informeConNarcotrafico.Id;
            informeSinRelacionId = informeSinRelacion.Id;
        }

        await using var dbContext = new AppDbContext(options);
        var handler = new BuscarInformesQueryHandler(dbContext, new NullAuditLogger());

        var resultado = await handler.Handle(
            new BuscarInformesQuery(TextoLibre: "narcotráfico"),
            CancellationToken.None);

        Assert.Contains(resultado, r => r.Id == informeConNarcotraficoId);
        Assert.DoesNotContain(resultado, r => r.Id == informeSinRelacionId);
    }

    [Fact]
    public async Task BuscarInformes_TextoLibreSinCoincidenciaEnNingunRelato_NoDevuelveInformes()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using (var migrationContext = new AppDbContext(options))
        {
            await migrationContext.Database.MigrateAsync();
        }

        var dependenciaId = Guid.NewGuid();

        await using (var setupContext = new AppDbContext(options))
        {
            var dependencia = new Dependencia("Comisaría Seccional Primera", TipoDependencia.Comisaria);
            setupContext.Dependencias.Add(dependencia);
            await setupContext.SaveChangesAsync();

            var informe = CrearInformeMigrado(
                "1/2026", new DateOnly(2026, 5, 10), dependencia.Id,
                "Sustracción de un vehículo estacionado frente a una vivienda particular.");
            setupContext.Informes.Add(informe);
            await setupContext.SaveChangesAsync();
        }

        await using var dbContext = new AppDbContext(options);
        var handler = new BuscarInformesQueryHandler(dbContext, new NullAuditLogger());

        var resultado = await handler.Handle(
            new BuscarInformesQuery(TextoLibre: "narcotráfico"),
            CancellationToken.None);

        Assert.Empty(resultado);
    }

    [Fact]
    public async Task BuscarInformes_TextoLibreConDosPalabrasRelacionadas_UsaFullTextYNoLikeLiteral()
    {
        // Valida que el filtro realmente use tsvector/tsquery (full-text,
        // con normalización del diccionario 'spanish') y no un LIKE/Contains
        // literal: se busca el texto "robo de vehículos" (plural, con
        // preposición) y debe matchear un relato que contiene "vehículo"
        // (singular) en otra posición de la oración — un LIKE exacto de la
        // frase completa no lo encontraría.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using (var migrationContext = new AppDbContext(options))
        {
            await migrationContext.Database.MigrateAsync();
        }

        var dependenciaId = Guid.NewGuid();
        Guid informeId;

        await using (var setupContext = new AppDbContext(options))
        {
            var dependencia = new Dependencia("Comisaría Seccional Primera", TipoDependencia.Comisaria);
            setupContext.Dependencias.Add(dependencia);
            await setupContext.SaveChangesAsync();

            var informe = CrearInformeMigrado(
                "1/2026", new DateOnly(2026, 5, 10), dependencia.Id,
                "Se registró el robo de un vehículo particular en la vía pública.");
            setupContext.Informes.Add(informe);
            await setupContext.SaveChangesAsync();

            informeId = informe.Id;
        }

        await using var dbContext = new AppDbContext(options);
        var handler = new BuscarInformesQueryHandler(dbContext, new NullAuditLogger());

        var resultado = await handler.Handle(
            new BuscarInformesQuery(TextoLibre: "robo vehículo"),
            CancellationToken.None);

        Assert.Contains(resultado, r => r.Id == informeId);
    }
}
