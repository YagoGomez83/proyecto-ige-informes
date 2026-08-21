using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Application.Informes.Queries.ReprocesarMigracionPendiente;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Informes;

/// <summary>
/// HU-04 · Migración histórica de informes desde Drive
/// (docs/epic-01-gestion-informes.md) — reprocesar una MigracionPendiente
/// ya creada volviendo a leer su PDF ya guardado en MinIO con la versión
/// actual de InformePdfParser (ver skill pdf-informe-parser, fix del
/// formato "DEL" en la fecha). No persiste nada — solo devuelve lo que el
/// parser logre reconocer ahora.
/// </summary>
public class ReprocesarMigracionPendienteQueryHandlerTests
{
    private static async Task<(TestAppDbContext DbContext, FakeFileStorage FileStorage, MigracionPendiente MigracionPendiente)> PrepararAsync()
    {
        var dbContext = new TestAppDbContext();
        var dependencia = new Dependencia("Comisaría 2°", TipoDependencia.Comisaria);
        dbContext.Dependencias.Add(dependencia);
        await dbContext.SaveChangesAsync();

        var migracionPendiente = new MigracionPendiente(
            "111/2023",
            "migraciones-pendientes/111.pdf",
            dependencia.Id,
            Guid.NewGuid(),
            "AV.HURTO CALIFICADO",
            "111/2023",
            "Relato de prueba");

        dbContext.MigracionesPendientes.Add(migracionPendiente);
        await dbContext.SaveChangesAsync();

        var fileStorage = new FakeFileStorage();
        fileStorage.ContenidoPorClave[migracionPendiente.PdfPath] = [1, 2, 3];

        return (dbContext, fileStorage, migracionPendiente);
    }

    [Fact]
    public async Task ElParserAhoraReconoceLaFecha_DevuelveLaFechaSinPersistirNada()
    {
        var (dbContext, fileStorage, migracionPendiente) = await PrepararAsync();
        var extraido = new InformeExtraidoDto("111/2023", new DateOnly(2023, 8, 8), null, null, null, null, [], [], []);
        var handler = new ReprocesarMigracionPendienteQueryHandler(dbContext, fileStorage, new FakeInformePdfParser(extraido), new FakeAuditLogger());

        var resultado = await handler.Handle(new ReprocesarMigracionPendienteQuery(migracionPendiente.Id), CancellationToken.None);

        Assert.Equal("111/2023", resultado.IdRegistro);
        Assert.Equal(new DateOnly(2023, 8, 8), resultado.FechaAnalisis);
        Assert.Single(dbContext.MigracionesPendientes.ToList()); // no se tocó
    }

    [Fact]
    public async Task ElParserSigueSinReconocerNada_DevuelveAmbosCamposNulos()
    {
        var (dbContext, fileStorage, migracionPendiente) = await PrepararAsync();
        var extraido = new InformeExtraidoDto(null, null, null, null, null, null, [], [], []);
        var handler = new ReprocesarMigracionPendienteQueryHandler(dbContext, fileStorage, new FakeInformePdfParser(extraido), new FakeAuditLogger());

        var resultado = await handler.Handle(new ReprocesarMigracionPendienteQuery(migracionPendiente.Id), CancellationToken.None);

        Assert.Null(resultado.IdRegistro);
        Assert.Null(resultado.FechaAnalisis);
    }

    [Fact]
    public async Task MigracionPendienteInexistente_RechazaConEntidadNoEncontrada()
    {
        var dbContext = new TestAppDbContext();
        var fileStorage = new FakeFileStorage();
        var extraido = new InformeExtraidoDto(null, null, null, null, null, null, [], [], []);
        var handler = new ReprocesarMigracionPendienteQueryHandler(dbContext, fileStorage, new FakeInformePdfParser(extraido), new FakeAuditLogger());

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(
            () => handler.Handle(new ReprocesarMigracionPendienteQuery(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task PdfQueTardaMasQueElTimeout_RechazaConReglaDeNegocioViolada()
    {
        var (dbContext, fileStorage, migracionPendiente) = await PrepararAsync();
        var extraido = new InformeExtraidoDto("111/2023", new DateOnly(2023, 8, 8), null, null, null, null, [], [], []);
        var parser = new FakeInformePdfParserPorArchivo().ConDemora(TimeSpan.FromSeconds(5), extraido);
        var handler = new ReprocesarMigracionPendienteQueryHandler(dbContext, fileStorage, parser, new FakeAuditLogger(), timeoutParseo: TimeSpan.FromSeconds(1));

        await Assert.ThrowsAsync<ReglaDeNegocioVioladaException>(
            () => handler.Handle(new ReprocesarMigracionPendienteQuery(migracionPendiente.Id), CancellationToken.None));
    }

    [Fact]
    public async Task RegistraLaLecturaDelPdfEnAuditLog()
    {
        var (dbContext, fileStorage, migracionPendiente) = await PrepararAsync();
        var extraido = new InformeExtraidoDto("111/2023", new DateOnly(2023, 8, 8), null, null, null, null, [], [], []);
        var auditLogger = new FakeAuditLogger();
        var handler = new ReprocesarMigracionPendienteQueryHandler(dbContext, fileStorage, new FakeInformePdfParser(extraido), auditLogger);

        await handler.Handle(new ReprocesarMigracionPendienteQuery(migracionPendiente.Id), CancellationToken.None);

        Assert.Contains(auditLogger.Registros, r => r.Accion == "ReprocesarPdf" && r.Entidad == nameof(MigracionPendiente) && r.EntidadId == migracionPendiente.Id);
    }

    [Fact]
    public async Task ElPdfTieneDniDePersonas_RegistraAccesoAExtraccionDeDatosPersonales()
    {
        var (dbContext, fileStorage, migracionPendiente) = await PrepararAsync();
        var extraido = new InformeExtraidoDto(
            "111/2023", new DateOnly(2023, 8, 8), null, null, null, null, [],
            [new PersonaExtraidaDto("12345678", "Denunciante")], []);
        var auditLogger = new FakeAuditLogger();
        var handler = new ReprocesarMigracionPendienteQueryHandler(dbContext, fileStorage, new FakeInformePdfParser(extraido), auditLogger);

        await handler.Handle(new ReprocesarMigracionPendienteQuery(migracionPendiente.Id), CancellationToken.None);

        Assert.Contains(auditLogger.Registros, r => r.Accion == "ExtraccionPdf" && r.Entidad == nameof(Persona));
    }
}
