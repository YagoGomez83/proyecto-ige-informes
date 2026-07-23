using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Application.Dependencias.Commands.AsignarBarrioADependencia;
using IGE.Informes.Domain.Entities;
using IGE.Informes.Infrastructure.Auditing;
using IGE.Informes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace IGE.Informes.IntegrationTests.Dependencias;

/// <summary>
/// Confirma contra Postgres real que asignar un Barrio a una Dependencia ya
/// persistida (PrimitiveCollection.BarrioIds) traduce correctamente y queda
/// registrado en AuditLog vía el interceptor genérico — mismo cuidado que
/// AsignarCategoriaAlertaAuditLogTests (HU-08) para PrimitiveCollection
/// sobre una entidad ya trackeada.
/// </summary>
public class AsignarBarrioADependenciaAuditLogTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private sealed class FakeCurrentUserService(Guid usuarioId) : ICurrentUserService
    {
        public Guid? UsuarioId { get; } = usuarioId;
        public IReadOnlyCollection<string> Roles { get; } = ["Admin"];
    }

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact]
    public async Task Asignar_barrio_persiste_la_relacion_y_queda_en_AuditLog()
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

        Guid dependenciaId;
        Guid barrioId;

        await using (var setupContext = new AppDbContext(options))
        {
            var dependencia = new Dependencia("Comisaría Seccional Primera", TipoDependencia.Comisaria);
            var barrio = new Barrio("Barrio Norte");
            setupContext.Dependencias.Add(dependencia);
            setupContext.Barrios.Add(barrio);
            await setupContext.SaveChangesAsync();

            dependenciaId = dependencia.Id;
            barrioId = barrio.Id;
        }

        var interceptor = new AuditLogInterceptor(currentUserService);
        var optionsConInterceptor = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .AddInterceptors(interceptor)
            .Options;

        await using (var dbContext = new AppDbContext(optionsConInterceptor))
        {
            var handler = new AsignarBarrioADependenciaCommandHandler(dbContext);

            await handler.Handle(new AsignarBarrioADependenciaCommand(dependenciaId, barrioId), CancellationToken.None);
        }

        await using (var assertContext = new AppDbContext(options))
        {
            var dependenciaActualizada = await assertContext.Dependencias.FindAsync(dependenciaId);
            Assert.Contains(barrioId, dependenciaActualizada!.BarrioIds);

            var registroAuditoria = await assertContext.AuditLogs
                .Where(a => a.Entidad == nameof(Dependencia) && a.EntidadId == dependenciaId)
                .OrderByDescending(a => a.Timestamp)
                .FirstOrDefaultAsync();

            Assert.NotNull(registroAuditoria);
            Assert.Equal("Modificacion", registroAuditoria.Accion);
            Assert.Equal(usuarioId, registroAuditoria.UsuarioId);
        }
    }

    [Fact]
    public async Task Crear_dependencia_queda_registrado_como_Alta_en_AuditLog()
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

        var interceptor = new AuditLogInterceptor(currentUserService);
        var optionsConInterceptor = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .AddInterceptors(interceptor)
            .Options;

        Guid dependenciaId;

        await using (var dbContext = new AppDbContext(optionsConInterceptor))
        {
            var handler = new IGE.Informes.Application.Dependencias.Commands.CrearDependencia.CrearDependenciaCommandHandler(dbContext);

            dependenciaId = await handler.Handle(
                new IGE.Informes.Application.Dependencias.Commands.CrearDependencia.CrearDependenciaCommand(
                    "Comisaría Seccional Primera", TipoDependencia.Comisaria),
                CancellationToken.None);
        }

        await using (var assertContext = new AppDbContext(options))
        {
            var registroAuditoria = await assertContext.AuditLogs
                .Where(a => a.Entidad == nameof(Dependencia) && a.EntidadId == dependenciaId)
                .OrderByDescending(a => a.Timestamp)
                .FirstOrDefaultAsync();

            Assert.NotNull(registroAuditoria);
            Assert.Equal("Alta", registroAuditoria.Accion);
            Assert.Equal(usuarioId, registroAuditoria.UsuarioId);
        }
    }
}
