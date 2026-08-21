using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Application.Informes.Commands.ConfirmarCargaInforme;
using IGE.Informes.Domain.Entities;
using IGE.Informes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace IGE.Informes.IntegrationTests.Informes;

public class ConfirmarCargaInformeReemplazoTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private sealed class FakeCurrentUserService(Guid usuarioId) : ICurrentUserService
    {
        public Guid? UsuarioId { get; } = usuarioId;
        public IReadOnlyCollection<string> Roles { get; } = ["Analista"];
    }

    private sealed class FakeFileStorage : IFileStorage
    {
        public Task<string> SubirAsync(string nombreArchivo, Stream contenido, string tipoMime, CancellationToken cancellationToken = default) =>
            Task.FromResult($"fake/{Guid.NewGuid():N}/{nombreArchivo}");

        public Task<string> ObtenerUrlDescargaAsync(string clave, CancellationToken cancellationToken = default) =>
            Task.FromResult($"https://fake.local/{clave}");

        public Task<byte[]> DescargarAsync(string clave, CancellationToken cancellationToken = default) =>
            Task.FromResult(Array.Empty<byte>());

        public Task EliminarAsync(string clave, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeAntivirusScanner : IAntivirusScanner
    {
        public Task<bool> EstaLimpioAsync(byte[] contenido, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact]
    public async Task Reemplazar_un_Informe_existente_contra_Postgres_real_actualiza_Causa_y_Evidencias_sin_duplicar()
    {
        var usuarioId = Guid.NewGuid();
        var currentUserService = new FakeCurrentUserService(usuarioId);
        var fileStorage = new FakeFileStorage();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using (var migrationContext = new AppDbContext(options))
        {
            await migrationContext.Database.MigrateAsync();
        }

        Guid casoId;
        Guid dependenciaId;

        await using (var setupContext = new AppDbContext(options))
        {
            var caso = new CasoAnalisis(new DateOnly(2026, 7, 21), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            var dependencia = new Dependencia("Comisaría 2°", TipoDependencia.Comisaria);
            setupContext.CasosAnalisis.Add(caso);
            setupContext.Dependencias.Add(dependencia);
            await setupContext.SaveChangesAsync();

            casoId = caso.Id;
            dependenciaId = dependencia.Id;
        }

        var contenidoPdf = "%PDF-1.4 primera version"u8.ToArray();
        var comandoInicial = new ConfirmarCargaInformeCommand(
            contenidoPdf, "290-2026.pdf", "290/2026", new DateOnly(2026, 7, 14), casoId, dependenciaId,
            "N.N. s/Robo", "7070029/26", "Primera Circunscripción", "Relato inicial", false,
            [new(1, null, null, "Primera evidencia")]);

        Guid informeId;
        await using (var dbContext = new AppDbContext(options))
        {
            var handler = new ConfirmarCargaInformeCommandHandler(dbContext, currentUserService, fileStorage, new FakeAntivirusScanner());
            informeId = await handler.Handle(comandoInicial, CancellationToken.None);
        }

        var comandoReemplazo = comandoInicial with
        {
            Relato = "Relato corregido tras revisión manual",
            ReemplazarSiExiste = true,
            Evidencias = [new(1, null, null, "Evidencia corregida"), new(2, null, null, "Segunda evidencia nueva")],
        };

        await using (var dbContext = new AppDbContext(options))
        {
            var handler = new ConfirmarCargaInformeCommandHandler(dbContext, currentUserService, fileStorage, new FakeAntivirusScanner());
            var informeIdReemplazo = await handler.Handle(comandoReemplazo, CancellationToken.None);

            Assert.Equal(informeId, informeIdReemplazo);
        }

        await using (var assertContext = new AppDbContext(options))
        {
            var informe = await assertContext.Informes.FindAsync(informeId);
            Assert.Equal("Relato corregido tras revisión manual", informe!.Relato);

            var evidencias = await assertContext.Evidencias.Where(e => e.InformeId == informeId).ToListAsync();
            Assert.Equal(2, evidencias.Count);
            Assert.Contains(evidencias, e => e.Descripcion == "Evidencia corregida");
            Assert.Contains(evidencias, e => e.Descripcion == "Segunda evidencia nueva");
        }
    }
}
