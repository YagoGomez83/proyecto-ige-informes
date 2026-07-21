using IGE.Informes.Application.CasosAnalisis.Commands.RegistrarCaso;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using IGE.Informes.Infrastructure.Auditing;
using IGE.Informes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace IGE.Informes.IntegrationTests.CasosAnalisis;

public class RegistrarCasoAuditLogTests : IAsyncLifetime
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
    public async Task Registrar_un_caso_deja_constancia_en_AuditLog()
    {
        var usuarioId = Guid.NewGuid();
        var currentUserService = new FakeCurrentUserService(usuarioId);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        var interceptor = new AuditLogInterceptor(currentUserService);

        await using (var migrationContext = new AppDbContext(options))
        {
            await migrationContext.Database.MigrateAsync();
        }

        Guid casoId;
        Guid dependenciaId;
        Guid tipoIncidenteId;

        await using (var setupContext = new AppDbContext(options))
        {
            var dependencia = new Dependencia("Comisaría 2°", TipoDependencia.Comisaria);
            var tipoIncidente = new TipoIncidente("164", "ROBO");
            setupContext.Dependencias.Add(dependencia);
            setupContext.TiposIncidente.Add(tipoIncidente);
            await setupContext.SaveChangesAsync();

            dependenciaId = dependencia.Id;
            tipoIncidenteId = tipoIncidente.Id;
        }

        var optionsConInterceptor = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .AddInterceptors(interceptor)
            .Options;

        await using (var dbContext = new AppDbContext(optionsConInterceptor))
        {
            var handler = new RegistrarCasoCommandHandler(dbContext, currentUserService);

            casoId = await handler.Handle(
                new RegistrarCasoCommand(new DateOnly(2026, 7, 21), dependenciaId, tipoIncidenteId, "911-1234", "Observación"),
                CancellationToken.None);
        }

        await using (var assertContext = new AppDbContext(options))
        {
            var registroAuditoria = await assertContext.AuditLogs
                .SingleOrDefaultAsync(a => a.Entidad == nameof(CasoAnalisis) && a.EntidadId == casoId);

            Assert.NotNull(registroAuditoria);
            Assert.Equal("Alta", registroAuditoria.Accion);
            Assert.Equal(usuarioId, registroAuditoria.UsuarioId);
        }
    }
}
