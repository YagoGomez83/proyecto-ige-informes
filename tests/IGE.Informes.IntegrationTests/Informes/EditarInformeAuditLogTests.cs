using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Application.Informes.Commands.EditarInforme;
using IGE.Informes.Domain.Entities;
using IGE.Informes.Infrastructure.Auditing;
using IGE.Informes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace IGE.Informes.IntegrationTests.Informes;

/// <summary>
/// HU-02 · Editar / corregir metadatos de un informe (Épica 01), escenario
/// "Corrección de campos": "los cambios se guardan y quedan registrados en
/// el log de auditoría con mi usuario y fecha/hora". Se verifica contra
/// Postgres real con AuditLogInterceptor — mismo patrón que
/// RegistrarCasoAuditLogTests y AsignarCategoriaAlertaAuditLogTests.
/// </summary>
public class EditarInformeAuditLogTests : IAsyncLifetime
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
    public async Task EditarInforme_CorreccionDeCamposEnBorrador_QuedaRegistradaEnAuditLogConUsuarioYFechaHora()
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
        Guid dependenciaOriginalId;
        Guid dependenciaNuevaId;

        await using (var setupContext = new AppDbContext(options))
        {
            var caso = new CasoAnalisis(new DateOnly(2026, 7, 21), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            var dependenciaOriginal = new Dependencia("Comisaría 2°", TipoDependencia.Comisaria);
            var dependenciaNueva = new Dependencia("Comisaría 5°", TipoDependencia.Comisaria);
            var causa = new Causa("N.N. s/Robo", "7070029/26", "Primera Circunscripción");

            setupContext.CasosAnalisis.Add(caso);
            setupContext.Dependencias.Add(dependenciaOriginal);
            setupContext.Dependencias.Add(dependenciaNueva);
            setupContext.Causas.Add(causa);
            await setupContext.SaveChangesAsync();

            var informe = new Informe("290/2026", new DateOnly(2026, 7, 14), caso.Id, dependenciaOriginal.Id, usuarioId, causa.Id);
            informe.CompletarRelato("Relato original.");
            setupContext.Informes.Add(informe);
            await setupContext.SaveChangesAsync();

            informeId = informe.Id;
            dependenciaOriginalId = dependenciaOriginal.Id;
            dependenciaNuevaId = dependenciaNueva.Id;
        }

        var antesDeEditar = DateTime.UtcNow;

        var interceptor = new AuditLogInterceptor(currentUserService);
        var optionsConInterceptor = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .AddInterceptors(interceptor)
            .Options;

        await using (var dbContext = new AppDbContext(optionsConInterceptor))
        {
            var handler = new EditarInformeCommandHandler(dbContext, currentUserService);

            var command = new EditarInformeCommand(
                informeId,
                Relato: "Relato corregido tras revisión manual.",
                DependenciaDestinoId: dependenciaNuevaId,
                CausaCaratula: "AV. INFRACCION LEY 23.737",
                CausaNroPiezaSumarial: "7070099/26",
                CausaCircunscripcionJudicial: "Segunda Circunscripción");

            await handler.Handle(command, CancellationToken.None);
        }

        var despuesDeEditar = DateTime.UtcNow;

        await using (var assertContext = new AppDbContext(options))
        {
            var informeActualizado = await assertContext.Informes.FindAsync(informeId);
            Assert.NotNull(informeActualizado);
            Assert.Equal("Relato corregido tras revisión manual.", informeActualizado.Relato);
            Assert.Equal(dependenciaNuevaId, informeActualizado.DependenciaDestinoId);
            Assert.NotEqual(dependenciaOriginalId, informeActualizado.DependenciaDestinoId);

            var registroAuditoria = await assertContext.AuditLogs
                .Where(a => a.Entidad == nameof(Informe) && a.EntidadId == informeId)
                .OrderByDescending(a => a.Timestamp)
                .FirstOrDefaultAsync();

            Assert.NotNull(registroAuditoria);
            Assert.Equal("Modificacion", registroAuditoria.Accion);
            Assert.Equal(usuarioId, registroAuditoria.UsuarioId);
            Assert.InRange(registroAuditoria.Timestamp, antesDeEditar.AddSeconds(-1), despuesDeEditar.AddSeconds(1));

            var causaNuevaId = informeActualizado.CausaId;
            var registroAltaCausa = await assertContext.AuditLogs
                .Where(a => a.Entidad == nameof(Causa) && a.EntidadId == causaNuevaId)
                .OrderByDescending(a => a.Timestamp)
                .FirstOrDefaultAsync();

            Assert.NotNull(registroAltaCausa);
            Assert.Equal("Alta", registroAltaCausa.Accion);
            Assert.Equal(usuarioId, registroAltaCausa.UsuarioId);
        }
    }
}
