using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Application.Informes.Commands.PublicarInforme;
using IGE.Informes.Domain.Entities;
using IGE.Informes.Infrastructure.Auditing;
using IGE.Informes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace IGE.Informes.IntegrationTests.Informes;

/// <summary>
/// HU-03 · Publicar / firmar un informe (Épica 01), escenario "Publicación
/// exitosa": "su estado cambia a Publicado" debe quedar registrado en el
/// log de auditoría con usuario y fecha/hora, igual que cualquier
/// modificación de Informe. Se verifica contra Postgres real con
/// AuditLogInterceptor — mismo patrón exacto que EditarInformeAuditLogTests.
///
/// También cubre acá (no en UnitTests) los escenarios que agregan un
/// InformeAnalista vía Add posterior a un SaveChanges previo del mismo
/// Informe (owned collection) — EF Core InMemory tiene una limitación
/// conocida ahí (DbUpdateConcurrencyException espuria) que no ocurre
/// contra Postgres real.
/// </summary>
public class PublicarInformeAuditLogTests : IAsyncLifetime
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
    public async Task PublicarInforme_PublicacionExitosa_QuedaRegistradaEnAuditLogConUsuarioYFechaHora()
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

        await using (var setupContext = new AppDbContext(options))
        {
            var caso = new CasoAnalisis(new DateOnly(2026, 7, 21), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            var dependencia = new Dependencia("Comisaría 2°", TipoDependencia.Comisaria);
            var causa = new Causa("N.N. s/Robo", "7070029/26", "Primera Circunscripción");

            setupContext.CasosAnalisis.Add(caso);
            setupContext.Dependencias.Add(dependencia);
            setupContext.Causas.Add(causa);
            await setupContext.SaveChangesAsync();

            var informe = new Informe("290/2026", new DateOnly(2026, 7, 14), caso.Id, dependencia.Id, usuarioId, causa.Id);
            informe.CompletarRelato("Relato original.");
            setupContext.Informes.Add(informe);
            await setupContext.SaveChangesAsync();

            informeId = informe.Id;
        }

        var antesDePublicar = DateTime.UtcNow;

        var interceptor = new AuditLogInterceptor(currentUserService);
        var optionsConInterceptor = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .AddInterceptors(interceptor)
            .Options;

        await using (var dbContext = new AppDbContext(optionsConInterceptor))
        {
            var handler = new PublicarInformeCommandHandler(dbContext, currentUserService);

            var command = new PublicarInformeCommand(informeId);

            await handler.Handle(command, CancellationToken.None);
        }

        var despuesDePublicar = DateTime.UtcNow;

        await using (var assertContext = new AppDbContext(options))
        {
            var informeActualizado = await assertContext.Informes.FindAsync(informeId);
            Assert.NotNull(informeActualizado);
            Assert.Equal(EstadoInforme.Publicado, informeActualizado.Estado);

            var registroAuditoria = await assertContext.AuditLogs
                .Where(a => a.Entidad == nameof(Informe) && a.EntidadId == informeId)
                .OrderByDescending(a => a.Timestamp)
                .FirstOrDefaultAsync();

            Assert.NotNull(registroAuditoria);
            Assert.Equal("Modificacion", registroAuditoria.Accion);
            Assert.Equal(usuarioId, registroAuditoria.UsuarioId);
            Assert.InRange(registroAuditoria.Timestamp, antesDePublicar.AddSeconds(-1), despuesDePublicar.AddSeconds(1));
        }
    }

    [Fact]
    public async Task PublicarInforme_UsuarioActualAunNoFirmante_LoAgregaComoFirmanteYPublica()
    {
        var usuarioSolicitanteId = Guid.NewGuid();
        var usuarioActualId = Guid.NewGuid();
        var currentUserService = new FakeCurrentUserService(usuarioActualId);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using (var migrationContext = new AppDbContext(options))
        {
            await migrationContext.Database.MigrateAsync();
        }

        Guid informeId;

        await using (var setupContext = new AppDbContext(options))
        {
            var caso = new CasoAnalisis(new DateOnly(2026, 7, 21), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            var dependencia = new Dependencia("Comisaría 2°", TipoDependencia.Comisaria);
            var causa = new Causa("N.N. s/Robo", "7070029/26", "Primera Circunscripción");

            setupContext.CasosAnalisis.Add(caso);
            setupContext.Dependencias.Add(dependencia);
            setupContext.Causas.Add(causa);
            await setupContext.SaveChangesAsync();

            var informe = new Informe("290/2026", new DateOnly(2026, 7, 14), caso.Id, dependencia.Id, usuarioSolicitanteId, causa.Id);
            setupContext.Informes.Add(informe);
            await setupContext.SaveChangesAsync();

            informeId = informe.Id;
        }

        await using (var dbContext = new AppDbContext(options))
        {
            var handler = new PublicarInformeCommandHandler(dbContext, currentUserService);
            await handler.Handle(new PublicarInformeCommand(informeId), CancellationToken.None);
        }

        await using (var assertContext = new AppDbContext(options))
        {
            var informeActualizado = await assertContext.Informes.FindAsync(informeId);
            Assert.NotNull(informeActualizado);
            Assert.Equal(EstadoInforme.Publicado, informeActualizado.Estado);
            Assert.Contains(
                informeActualizado.Analistas,
                a => a.UsuarioId == usuarioActualId && a.Rol == RolInformeAnalista.Firmante);
        }
    }

    [Fact]
    public async Task PublicarInforme_InformeYaPublicado_EsNoOpYNoAgregaNuevosFirmantes()
    {
        var usuarioSolicitanteId = Guid.NewGuid();
        var firmanteOriginalId = Guid.NewGuid();
        var otroUsuarioId = Guid.NewGuid();
        var currentUserService = new FakeCurrentUserService(otroUsuarioId);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using (var migrationContext = new AppDbContext(options))
        {
            await migrationContext.Database.MigrateAsync();
        }

        Guid informeId;

        await using (var setupContext = new AppDbContext(options))
        {
            var caso = new CasoAnalisis(new DateOnly(2026, 7, 21), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            var dependencia = new Dependencia("Comisaría 2°", TipoDependencia.Comisaria);
            var causa = new Causa("N.N. s/Robo", "7070029/26", "Primera Circunscripción");

            setupContext.CasosAnalisis.Add(caso);
            setupContext.Dependencias.Add(dependencia);
            setupContext.Causas.Add(causa);
            await setupContext.SaveChangesAsync();

            var informe = new Informe("290/2026", new DateOnly(2026, 7, 14), caso.Id, dependencia.Id, usuarioSolicitanteId, causa.Id);
            informe.AgregarFirmante(firmanteOriginalId);
            informe.Publicar();
            setupContext.Informes.Add(informe);
            await setupContext.SaveChangesAsync();

            informeId = informe.Id;
        }

        await using (var dbContext = new AppDbContext(options))
        {
            var handler = new PublicarInformeCommandHandler(dbContext, currentUserService);
            var excepcion = await Record.ExceptionAsync(
                () => handler.Handle(new PublicarInformeCommand(informeId), CancellationToken.None));

            Assert.Null(excepcion);
        }

        await using (var assertContext = new AppDbContext(options))
        {
            var informeActualizado = await assertContext.Informes.FindAsync(informeId);
            Assert.NotNull(informeActualizado);
            Assert.Equal(EstadoInforme.Publicado, informeActualizado.Estado);
            Assert.DoesNotContain(informeActualizado.Analistas, a => a.UsuarioId == otroUsuarioId);
            Assert.Contains(
                informeActualizado.Analistas,
                a => a.UsuarioId == firmanteOriginalId && a.Rol == RolInformeAnalista.Firmante);
        }
    }
}
