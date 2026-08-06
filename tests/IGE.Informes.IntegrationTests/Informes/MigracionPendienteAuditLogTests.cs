using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Application.Informes.Commands.CrearInformeDesdeMigracionPendiente;
using IGE.Informes.Application.Informes.Commands.MigrarInformes;
using IGE.Informes.Domain.Entities;
using IGE.Informes.Infrastructure.Auditing;
using IGE.Informes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace IGE.Informes.IntegrationTests.Informes;

public class MigracionPendienteAuditLogTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private sealed class FakeCurrentUserService(Guid usuarioId) : ICurrentUserService
    {
        public Guid? UsuarioId { get; } = usuarioId;
        public IReadOnlyCollection<string> Roles { get; } = ["Admin"];
    }

    private sealed class FakeFileStorage : IFileStorage
    {
        public Task<string> SubirAsync(string nombreArchivo, Stream contenido, string tipoMime, CancellationToken cancellationToken = default) =>
            Task.FromResult($"migraciones-pendientes/{Guid.NewGuid():N}/{nombreArchivo}");

        public Task<string> ObtenerUrlDescargaAsync(string clave, CancellationToken cancellationToken = default) =>
            Task.FromResult($"https://fake.local/{clave}");

        public Task EliminarAsync(string clave, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeAntivirusScanner : IAntivirusScanner
    {
        public Task<bool> EstaLimpioAsync(byte[] contenido, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    private sealed class FakeInformePdfParser(InformeExtraidoDto resultado) : IInformePdfParser
    {
        public InformeExtraidoDto Parsear(Stream pdfStream) => resultado;
    }

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact]
    public async Task MigrarSinFechaYCompletarDespues_contra_Postgres_real_persiste_el_PDF_como_MigracionPendiente_y_luego_crea_el_Informe_con_AuditLog()
    {
        var usuarioId = Guid.NewGuid();
        var currentUserService = new FakeCurrentUserService(usuarioId);
        var fileStorage = new FakeFileStorage();
        var antivirusScanner = new FakeAntivirusScanner();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using (var migrationContext = new AppDbContext(options))
        {
            await migrationContext.Database.MigrateAsync();
        }

        Guid dependenciaId;
        await using (var setupContext = new AppDbContext(options))
        {
            var dependencia = new Dependencia("Comisaría 9°", TipoDependencia.Comisaria);
            setupContext.Dependencias.Add(dependencia);
            await setupContext.SaveChangesAsync();

            dependenciaId = dependencia.Id;
        }

        var interceptor = new AuditLogInterceptor(currentUserService);
        var optionsConInterceptor = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .AddInterceptors(interceptor)
            .Options;

        var extraidoSinFecha = new InformeExtraidoDto(
            "900/2022", null, "AV. INFRACCION LEY 23.737", null, "7070029/26",
            "Se procede a realizar el análisis histórico...", [], [], []);
        var parser = new FakeInformePdfParser(extraidoSinFecha);
        var contenidoPdf = "%PDF-1.4 informe historico sin fecha"u8.ToArray();

        Guid migracionPendienteId;
        await using (var dbContext = new AppDbContext(optionsConInterceptor))
        {
            var handler = new MigrarInformesCommandHandler(
                dbContext, currentUserService, parser, new NullAuditLogger(), fileStorage, antivirusScanner);

            var reporte = await handler.Handle(
                new MigrarInformesCommand(dependenciaId, [new PdfMigrarDto(contenidoPdf, "900-2022.pdf")]),
                CancellationToken.None);

            Assert.Equal(1, reporte.ConAdvertencia);
        }

        await using (var assertContext = new AppDbContext(options))
        {
            var migracionPendiente = await assertContext.MigracionesPendientes
                .FirstOrDefaultAsync(m => m.IdRegistro == "900/2022");

            Assert.NotNull(migracionPendiente);
            Assert.StartsWith("migraciones-pendientes/", migracionPendiente!.PdfPath);
            Assert.Equal("7070029/26", migracionPendiente.PiezaSumarial);

            migracionPendienteId = migracionPendiente.Id;
        }

        Guid informeId;
        await using (var dbContext = new AppDbContext(optionsConInterceptor))
        {
            var handler = new CrearInformeDesdeMigracionPendienteCommandHandler(dbContext, currentUserService, new NullAuditLogger());

            informeId = await handler.Handle(
                new CrearInformeDesdeMigracionPendienteCommand(migracionPendienteId, new DateOnly(2022, 9, 9)),
                CancellationToken.None);
        }

        await using (var assertContext = new AppDbContext(options))
        {
            var informe = await assertContext.Informes.FindAsync(informeId);
            Assert.NotNull(informe);
            Assert.Equal("900/2022", informe!.IdRegistro);
            Assert.Equal(new DateOnly(2022, 9, 9), informe.FechaAnalisis);
            Assert.Equal(OrigenInforme.Migrado, informe.Origen);

            Assert.Empty(await assertContext.MigracionesPendientes.ToListAsync());

            var registroAuditoria = await assertContext.AuditLogs
                .Where(a => a.Entidad == nameof(Informe) && a.EntidadId == informeId)
                .OrderByDescending(a => a.Timestamp)
                .FirstOrDefaultAsync();

            Assert.NotNull(registroAuditoria);
            Assert.Equal("Alta", registroAuditoria!.Accion);
            Assert.Equal(usuarioId, registroAuditoria.UsuarioId);

            var registroBajaMigracionPendiente = await assertContext.AuditLogs
                .Where(a => a.Entidad == nameof(MigracionPendiente) && a.EntidadId == migracionPendienteId)
                .OrderByDescending(a => a.Timestamp)
                .FirstOrDefaultAsync();

            Assert.NotNull(registroBajaMigracionPendiente);
            Assert.Equal("Baja", registroBajaMigracionPendiente!.Accion);
        }
    }

    [Fact]
    public async Task DosAdminsCompletandoLaMismaMigracionPendienteSimultaneamente_contra_Postgres_real_elSegundoRechazaPorConcurrencia()
    {
        // Hallazgo del security-reviewer: sin un concurrency token sobre
        // xmin, dos Admins completando la misma MigracionPendiente (doble
        // click, dos pestañas) no chocaban entre sí — "ganaba" el último
        // SaveChangesAsync en silencio, con la fecha equivocada aceptada
        // sin error. Este test simula la carrera con dos DbContext
        // independientes que leen la misma fila antes de que ninguno
        // confirme, contra Postgres real (no InMemory, donde xmin no se
        // comporta igual).
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
        await using (var setupContext = new AppDbContext(options))
        {
            var dependencia = new Dependencia("Comisaría 10°", TipoDependencia.Comisaria);
            setupContext.Dependencias.Add(dependencia);
            await setupContext.SaveChangesAsync();

            dependenciaId = dependencia.Id;
        }

        Guid migracionPendienteId;
        await using (var setupContext = new AppDbContext(options))
        {
            var migracionPendiente = new MigracionPendiente(
                "950/2022", "migraciones-pendientes/950-2022.pdf", dependenciaId, Guid.NewGuid());
            setupContext.MigracionesPendientes.Add(migracionPendiente);
            await setupContext.SaveChangesAsync();

            migracionPendienteId = migracionPendiente.Id;
        }

        // Dos DbContext independientes simulan dos requests concurrentes
        // que corren en paralelo real (Task.WhenAll, no secuencial): ambos
        // Handlers leen la MigracionPendiente mientras todavía existe,
        // antes de que cualquiera confirme su SaveChangesAsync — la
        // ventana exacta que motivó agregar xmin como concurrency token
        // (hallazgo del security-reviewer). Uno de los dos gana; el otro
        // debe rechazar, nunca los dos completar silenciosamente con
        // fechas distintas.
        var command = new CrearInformeDesdeMigracionPendienteCommand(migracionPendienteId, new DateOnly(2022, 6, 1));

        async Task<Exception?> EjecutarYCapturarAsync()
        {
            await using var dbContext = new AppDbContext(options);
            var handler = new CrearInformeDesdeMigracionPendienteCommandHandler(dbContext, currentUserService, new NullAuditLogger());

            try
            {
                await handler.Handle(command, CancellationToken.None);
                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        var resultados = await Task.WhenAll(EjecutarYCapturarAsync(), EjecutarYCapturarAsync());

        // El mecanismo exacto que detecta al perdedor depende del timing
        // real de la carrera: choque de xmin, o el índice único de
        // Informe.IdRegistro si ambos pasaron el chequeo AnyAsync antes de
        // que cualquiera confirmara — el Handler traduce cualquiera de los
        // dos (DbUpdateException, clase base) al mismo InvalidOperationException
        // con mensaje claro. Alternativamente, si la carrera corre lo
        // bastante secuencial, el perdedor puede no encontrar la fila
        // (EntidadNoEncontradaException) o detectar el duplicado antes de
        // llegar a guardar (EntidadDuplicadaException). Lo que importa es
        // que exactamente uno de los dos gane, nunca los dos.
        Assert.Single(resultados, r => r is null);
        var excepcionDelPerdedor = Assert.Single(resultados, r => r is not null);
        Assert.True(
            excepcionDelPerdedor is InvalidOperationException or EntidadNoEncontradaException or EntidadDuplicadaException,
            $"Se esperaba un rechazo del perdedor (concurrencia, fila ya borrada, o duplicado), no {excepcionDelPerdedor?.GetType().Name}.");

        await using var assertContext = new AppDbContext(options);
        Assert.Empty(await assertContext.MigracionesPendientes.ToListAsync());
        Assert.Single(await assertContext.Informes.Where(i => i.IdRegistro == "950/2022").ToListAsync());
    }

    [Fact]
    public async Task MigrarSinIdRegistroYCompletarDespues_contra_Postgres_real_persiste_la_MigracionPendiente_con_IdRegistro_nulo_y_luego_crea_el_Informe()
    {
        // HU-04, escenarios "PDF con ID Registro no reconocido también
        // queda pendiente, no se pierde" y "Completar el ID Registro de
        // una Migración Pendiente". Contra Postgres real porque el índice
        // único parcial de MigracionPendiente.IdRegistro (WHERE
        // "IdRegistro" IS NOT NULL) es justamente el tipo de comportamiento
        // de base de datos real que no conviene confirmar solo con
        // InMemory/mocks (ver docs/03-modelo-dominio.md, "Decisiones ya
        // resueltas").
        var usuarioId = Guid.NewGuid();
        var currentUserService = new FakeCurrentUserService(usuarioId);
        var fileStorage = new FakeFileStorage();
        var antivirusScanner = new FakeAntivirusScanner();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using (var migrationContext = new AppDbContext(options))
        {
            await migrationContext.Database.MigrateAsync();
        }

        Guid dependenciaId;
        await using (var setupContext = new AppDbContext(options))
        {
            var dependencia = new Dependencia("Comisaría 11°", TipoDependencia.Comisaria);
            setupContext.Dependencias.Add(dependencia);
            await setupContext.SaveChangesAsync();

            dependenciaId = dependencia.Id;
        }

        var interceptor = new AuditLogInterceptor(currentUserService);
        var optionsConInterceptor = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .AddInterceptors(interceptor)
            .Options;

        var extraidoSinIdRegistro = new InformeExtraidoDto(
            null, new DateOnly(2022, 3, 10), "AV. INFRACCION LEY 23.737", null, "7070029/26",
            "Relato sin ID Registro reconocido", [], [], []);
        var parser = new FakeInformePdfParser(extraidoSinIdRegistro);
        var contenidoPdf = "%PDF-1.4 informe historico sin id registro"u8.ToArray();

        Guid migracionPendienteId;
        await using (var dbContext = new AppDbContext(optionsConInterceptor))
        {
            var handler = new MigrarInformesCommandHandler(
                dbContext, currentUserService, parser, new NullAuditLogger(), fileStorage, antivirusScanner);

            var reporte = await handler.Handle(
                new MigrarInformesCommand(dependenciaId, [new PdfMigrarDto(contenidoPdf, "sin-id.pdf")]),
                CancellationToken.None);

            Assert.Equal(1, reporte.ConAdvertencia);
        }

        await using (var assertContext = new AppDbContext(options))
        {
            var migracionPendiente = await assertContext.MigracionesPendientes
                .FirstOrDefaultAsync(m => m.PiezaSumarial == "7070029/26");

            Assert.NotNull(migracionPendiente);
            Assert.Null(migracionPendiente!.IdRegistro);
            Assert.StartsWith("migraciones-pendientes/", migracionPendiente.PdfPath);

            migracionPendienteId = migracionPendiente.Id;
        }

        Guid informeId;
        await using (var dbContext = new AppDbContext(optionsConInterceptor))
        {
            var handler = new CrearInformeDesdeMigracionPendienteCommandHandler(dbContext, currentUserService, new NullAuditLogger());

            informeId = await handler.Handle(
                new CrearInformeDesdeMigracionPendienteCommand(migracionPendienteId, new DateOnly(2022, 9, 20), "950/2022"),
                CancellationToken.None);
        }

        await using (var assertContext = new AppDbContext(options))
        {
            var informe = await assertContext.Informes.FindAsync(informeId);
            Assert.NotNull(informe);
            Assert.Equal("950/2022", informe!.IdRegistro);
            Assert.Equal(new DateOnly(2022, 9, 20), informe.FechaAnalisis);
            Assert.Equal(OrigenInforme.Migrado, informe.Origen);

            Assert.Empty(await assertContext.MigracionesPendientes.ToListAsync());
        }
    }

    [Fact]
    public async Task DosMigracionesPendientesSinIdRegistro_contra_Postgres_real_conviven_gracias_al_indice_unico_parcial()
    {
        // Confirma explícitamente el diseño del índice único parcial
        // (WHERE "IdRegistro" IS NOT NULL): sin esa condición, Postgres
        // rechazaría la segunda fila con IdRegistro = null como si
        // chocara con la primera. Con el índice parcial, ambas conviven.
        var usuarioId = Guid.NewGuid();
        var currentUserService = new FakeCurrentUserService(usuarioId);
        var fileStorage = new FakeFileStorage();
        var antivirusScanner = new FakeAntivirusScanner();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using (var migrationContext = new AppDbContext(options))
        {
            await migrationContext.Database.MigrateAsync();
        }

        Guid dependenciaId;
        await using (var setupContext = new AppDbContext(options))
        {
            var dependencia = new Dependencia("Comisaría 12°", TipoDependencia.Comisaria);
            setupContext.Dependencias.Add(dependencia);
            await setupContext.SaveChangesAsync();

            dependenciaId = dependencia.Id;
        }

        var extraidoSinIdRegistro = new InformeExtraidoDto(
            null, new DateOnly(2022, 4, 1), null, null, null,
            "Relato A sin ID Registro", [], [], []);
        var parser = new FakeInformePdfParser(extraidoSinIdRegistro);
        var contenidoPdf = "%PDF-1.4 informe historico sin id registro"u8.ToArray();

        await using (var dbContext = new AppDbContext(options))
        {
            var handler = new MigrarInformesCommandHandler(
                dbContext, currentUserService, parser, new NullAuditLogger(), fileStorage, antivirusScanner);

            var reporte = await handler.Handle(
                new MigrarInformesCommand(dependenciaId, [
                    new PdfMigrarDto(contenidoPdf, "sin-id-a.pdf"),
                    new PdfMigrarDto(contenidoPdf, "sin-id-b.pdf")
                ]),
                CancellationToken.None);

            Assert.Equal(2, reporte.ConAdvertencia);
        }

        await using var assertContext = new AppDbContext(options);
        var migracionesPendientes = await assertContext.MigracionesPendientes.ToListAsync();
        Assert.Equal(2, migracionesPendientes.Count);
        Assert.All(migracionesPendientes, m => Assert.Null(m.IdRegistro));
    }

    private sealed class NullAuditLogger : IAuditLogger
    {
        public Task RegistrarAccesoAsync(string accion, string entidad, Guid? entidadId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
