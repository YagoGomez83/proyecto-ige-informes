using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Application.Personas.Queries.ObtenerHistorialInformesPersona;
using IGE.Informes.Domain.Entities;
using IGE.Informes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace IGE.Informes.IntegrationTests.Personas;

/// <summary>
/// HU-07 · Ficha 360° de una persona (Épica 02), contra Postgres real. Mismo
/// criterio que el equivalente de Vehículo
/// (IGE.Informes.IntegrationTests.Vehiculos.ObtenerHistorialInformesVehiculoQueryHandlerTests):
/// confirma que `e.PersonaIds.Contains(personaId)` con un Guid FIJO traduce
/// correctamente a SQL contra Npgsql.
/// </summary>
public class ObtenerHistorialInformesPersonaQueryHandlerTests : IAsyncLifetime
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
    public async Task ObtenerHistorial_FiltroPorPersonaId_TraduceCorrectamenteContraPostgresReal()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using (var migrationContext = new AppDbContext(options))
        {
            await migrationContext.Database.MigrateAsync();
        }

        Guid personaBuscadaId;
        Guid informeConPersonaId;

        await using (var setupContext = new AppDbContext(options))
        {
            var dependencia = new Dependencia("Comisaría 2°", TipoDependencia.Comisaria);
            setupContext.Dependencias.Add(dependencia);

            var personaBuscada = new Persona(RolPersona.Sospechoso, nombre: "Juan Pérez", dni: "30111222");
            var otraPersona = new Persona(RolPersona.Testigo, nombre: "María Gómez", dni: "28999888");
            setupContext.Personas.Add(personaBuscada);
            setupContext.Personas.Add(otraPersona);
            await setupContext.SaveChangesAsync();

            var informeConPersona = Informe.CrearMigrado("1/2026", new DateOnly(2026, 5, 10), dependencia.Id, Guid.NewGuid());
            var informeSinPersona = Informe.CrearMigrado("2/2026", new DateOnly(2026, 5, 11), dependencia.Id, Guid.NewGuid());
            setupContext.Informes.Add(informeConPersona);
            setupContext.Informes.Add(informeSinPersona);

            var evidenciaConPersonaBuscada = new Evidencia(1, informeConPersona.Id);
            evidenciaConPersonaBuscada.VincularPersona(personaBuscada.Id);
            setupContext.Evidencias.Add(evidenciaConPersonaBuscada);

            var evidenciaConOtraPersona = new Evidencia(1, informeSinPersona.Id);
            evidenciaConOtraPersona.VincularPersona(otraPersona.Id);
            setupContext.Evidencias.Add(evidenciaConOtraPersona);
            await setupContext.SaveChangesAsync();

            personaBuscadaId = personaBuscada.Id;
            informeConPersonaId = informeConPersona.Id;
        }

        await using var dbContext = new AppDbContext(options);
        var handler = new ObtenerHistorialInformesPersonaQueryHandler(dbContext, new NullAuditLogger());

        var resultado = await handler.Handle(
            new ObtenerHistorialInformesPersonaQuery(personaBuscadaId), CancellationToken.None);

        Assert.Single(resultado);
        Assert.Equal(informeConPersonaId, resultado.Single().InformeId);
    }

    [Fact]
    public async Task ObtenerHistorial_MultiplesInformesConMismaPersona_DevuelveOrdenadosPorFechaDescendenteContraPostgresReal()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using (var migrationContext = new AppDbContext(options))
        {
            await migrationContext.Database.MigrateAsync();
        }

        Guid personaId;
        Guid informeRecienteId;
        Guid informeAntiguoId;

        await using (var setupContext = new AppDbContext(options))
        {
            var dependencia = new Dependencia("Comisaría 2°", TipoDependencia.Comisaria);
            setupContext.Dependencias.Add(dependencia);

            var persona = new Persona(RolPersona.Sospechoso, nombre: "Juan Pérez", dni: "30111222");
            setupContext.Personas.Add(persona);
            await setupContext.SaveChangesAsync();

            var informeAntiguo = Informe.CrearMigrado("1/2026", new DateOnly(2026, 1, 10), dependencia.Id, Guid.NewGuid());
            var informeReciente = Informe.CrearMigrado("2/2026", new DateOnly(2026, 6, 15), dependencia.Id, Guid.NewGuid());
            setupContext.Informes.Add(informeAntiguo);
            setupContext.Informes.Add(informeReciente);

            var evidenciaAntigua = new Evidencia(1, informeAntiguo.Id);
            evidenciaAntigua.VincularPersona(persona.Id);
            setupContext.Evidencias.Add(evidenciaAntigua);

            var evidenciaReciente = new Evidencia(1, informeReciente.Id);
            evidenciaReciente.VincularPersona(persona.Id);
            setupContext.Evidencias.Add(evidenciaReciente);
            await setupContext.SaveChangesAsync();

            personaId = persona.Id;
            informeRecienteId = informeReciente.Id;
            informeAntiguoId = informeAntiguo.Id;
        }

        await using var dbContext = new AppDbContext(options);
        var handler = new ObtenerHistorialInformesPersonaQueryHandler(dbContext, new NullAuditLogger());

        var resultado = (await handler.Handle(
            new ObtenerHistorialInformesPersonaQuery(personaId), CancellationToken.None)).ToList();

        Assert.Equal(2, resultado.Count);
        Assert.Equal(informeRecienteId, resultado[0].InformeId);
        Assert.Equal(informeAntiguoId, resultado[1].InformeId);
    }
}
