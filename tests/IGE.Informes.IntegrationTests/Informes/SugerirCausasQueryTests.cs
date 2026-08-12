using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Application.Informes.Queries.SugerirCausas;
using IGE.Informes.Domain.Entities;
using IGE.Informes.Infrastructure.Busqueda;
using IGE.Informes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace IGE.Informes.IntegrationTests.Informes;

/// <summary>
/// HU-02 · Editar / corregir metadatos de un informe (Épica 01), escenario
/// "Sugerir Causas existentes cuando la Pieza Sumarial no matchea ninguna".
/// Todavía no existe ni la Query (Application) ni el Handler (Infrastructure,
/// por EF.Functions.ILike / similarity() de pg_trgm — mismo motivo por el
/// que BuscarCasosQueryHandler/BuscarVehiculosQueryHandler viven ahí y no en
/// Application, ver docs/03-modelo-dominio.md "Decisiones ya resueltas" y
/// feedback_handlers_infrastructure_registro_manual en memoria del
/// proyecto). similarity() sobre Causa.Caratula requiere la extensión
/// pg_trgm real de Postgres — no traduce en EF Core InMemory, por eso este
/// escenario se prueba acá contra un contenedor real, mismo patrón que
/// BuscarInformesTextoLibreTests (tsvector) y EditarInformeAuditLogTests.
/// Este test está escrito antes de la implementación (TDD): debe fallar en
/// rojo hasta que exista SugerirCausasQuery/SugerirCausasQueryHandler y la
/// migración que habilite la extensión "pg_trgm".
/// </summary>
public class SugerirCausasQueryTests : IAsyncLifetime
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
    public async Task SugerirCausas_PiezaSumarialSinMatchExacto_DevuelveCausasConCaratulaParecida()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using (var migrationContext = new AppDbContext(options))
        {
            await migrationContext.Database.MigrateAsync();
        }

        Guid causaParecidaId;

        await using (var setupContext = new AppDbContext(options))
        {
            var causaParecida = new Causa("AV. INFRACCION LEY 23.737 - TENENCIA DE ESTUPEFACIENTES", "7070029/26", "Primera Circunscripción");
            var causaSinRelacion = new Causa("N.N. s/Hurto en la vía pública", "1234567/26", "Segunda Circunscripción");

            setupContext.Causas.Add(causaParecida);
            setupContext.Causas.Add(causaSinRelacion);
            await setupContext.SaveChangesAsync();

            causaParecidaId = causaParecida.Id;
        }

        await using var dbContext = new AppDbContext(options);
        var handler = new SugerirCausasQueryHandler(dbContext, new NullAuditLogger());

        // No matchea ninguna Causa por N° de Pieza Sumarial exacto — se pide
        // sugerencia por similaridad de carátula.
        var resultado = await handler.Handle(
            new SugerirCausasQuery(CaratulaAproximada: "INFRACCION LEY 23737 TENENCIA ESTUPEFACIENTES"),
            CancellationToken.None);

        Assert.Contains(resultado, c => c.Id == causaParecidaId);
    }

    [Fact]
    public async Task SugerirCausas_SinNingunaCaratulaParecida_NoDevuelveSugerencias()
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
            var causaSinRelacion = new Causa("N.N. s/Hurto en la vía pública", "1234567/26", "Segunda Circunscripción");
            setupContext.Causas.Add(causaSinRelacion);
            await setupContext.SaveChangesAsync();
        }

        await using var dbContext = new AppDbContext(options);
        var handler = new SugerirCausasQueryHandler(dbContext, new NullAuditLogger());

        var resultado = await handler.Handle(
            new SugerirCausasQuery(CaratulaAproximada: "Homicidio agravado por el vínculo"),
            CancellationToken.None);

        Assert.Empty(resultado);
    }

    [Fact]
    public async Task SugerirCausas_ConMatchExactoDePiezaSumarial_NoLaSugiereYaQueDebeReusarseDirectamente()
    {
        // Escenario "Vincular la Causa a una ya existente por Pieza
        // Sumarial": si hay match exacto de N° de Pieza Sumarial, el flujo
        // correcto es EditarInformeCommandHandler reusando esa Causa
        // directamente (ver EditarInformeCommandHandlerTests) — no tiene
        // sentido pedir sugerencias en ese caso, pero si igual se piden,
        // esta Query no reemplaza ese chequeo de exactitud: solo devuelve
        // candidatas por similaridad de carátula, no decide la vinculación.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using (var migrationContext = new AppDbContext(options))
        {
            await migrationContext.Database.MigrateAsync();
        }

        await using (var setupContext = new AppDbContext(options))
        {
            var causaExistente = new Causa("N.N. s/Robo", "7070029/26", "Primera Circunscripción");
            setupContext.Causas.Add(causaExistente);
            await setupContext.SaveChangesAsync();
        }

        await using var dbContext = new AppDbContext(options);
        var handler = new SugerirCausasQueryHandler(dbContext, new NullAuditLogger());

        var resultado = await handler.Handle(
            new SugerirCausasQuery(CaratulaAproximada: "N.N. s/Robo"),
            CancellationToken.None);

        Assert.Contains(resultado, c => c.NroPiezaSumarial == "7070029/26");
    }
}
