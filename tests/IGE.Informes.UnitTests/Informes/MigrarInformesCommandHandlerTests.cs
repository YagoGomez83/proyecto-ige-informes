using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Application.Common.Security;
using IGE.Informes.Application.Informes.Commands.MigrarInformes;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Informes;

/// <summary>
/// HU-04 · Migración histórica de informes desde Drive
/// (docs/epic-01-gestion-informes.md) — Característica "Migración masiva",
/// escenarios "Migración por lote", "PDF con Fecha de Análisis no
/// reconocida queda pendiente, no se pierde" y "PDF con ID Registro no
/// reconocido también queda pendiente, no se pierde". Ambos escenarios de
/// "queda pendiente" cambian el comportamiento del Handler:
/// <see cref="MigrarInformesCommandHandler"/> recibe además
/// <see cref="IFileStorage"/> y <see cref="IAntivirusScanner"/> (mismo
/// puerto que ya usa ConfirmarCargaInformeCommandHandler) para poder subir
/// el PDF a MinIO y persistir una <see cref="MigracionPendiente"/> en vez
/// de descartar el archivo, ya sea que falte el ID Registro (queda
/// <c>IdRegistro = null</c>) o la Fecha de Análisis. Estos tests están
/// escritos antes de la implementación (TDD): deben fallar en rojo hasta
/// que se agregue esa dependencia nueva al constructor del Handler, se
/// cree IGE.Informes.Domain.Entities.MigracionPendiente, y el bloque
/// "if (extraido.RequiereRevisionManual)" del Handler deje de solo agregar
/// un detalle "Con advertencia" sin persistir.
/// </summary>
public class MigrarInformesCommandHandlerTests
{
    private static readonly Guid UsuarioMigradorId = Guid.NewGuid();
    private static readonly byte[] ContenidoPdfFalso = "%PDF-1.4 contenido de prueba"u8.ToArray();

    private static async Task<(TestAppDbContext DbContext, Dependencia Dependencia)> PrepararAsync()
    {
        var dbContext = new TestAppDbContext();
        var dependencia = new Dependencia("Comisaría 2°", TipoDependencia.Comisaria);

        dbContext.Dependencias.Add(dependencia);
        await dbContext.SaveChangesAsync();

        return (dbContext, dependencia);
    }

    private static MigrarInformesCommandHandler CrearHandler(
        TestAppDbContext dbContext,
        IInformePdfParser parser,
        IAuditLogger? auditLogger = null,
        IFileStorage? fileStorage = null,
        IAntivirusScanner? antivirusScanner = null,
        TimeSpan? timeoutPorArchivo = null) => new(
            dbContext,
            new FakeCurrentUserService(UsuarioMigradorId, Roles.Admin),
            parser,
            auditLogger ?? new FakeAuditLogger(),
            fileStorage ?? new FakeFileStorage(),
            antivirusScanner ?? new FakeAntivirusScanner(),
            timeoutPorArchivo);

    private static InformeExtraidoDto CrearExtraidoExitoso(string idRegistro, string? causaCaratula = null) => new(
        idRegistro,
        new DateOnly(2020, 3, 10),
        causaCaratula,
        "AV. INFRACCION LEY 23.737",
        causaCaratula is null ? null : "7070029/26",
        "Se procede a realizar el análisis histórico...",
        [],
        [],
        []);

    private static InformeExtraidoDto CrearExtraidoSinIdRegistro() => new(
        null,
        new DateOnly(2020, 3, 10),
        null,
        null,
        null,
        "Relato sin ID Registro reconocido",
        [],
        [],
        []);

    private static MigrarInformesCommand CrearCommand(Guid dependenciaId, params PdfMigrarDto[] pdfs) => new(
        dependenciaId,
        pdfs);

    [Fact]
    public async Task MigrarInformes_LoteConPdfsExitosos_PersisteTodosComoBorradorMigrado()
    {
        var (dbContext, dependencia) = await PrepararAsync();
        var parser = new FakeInformePdfParserPorArchivo()
            .ConResultado(CrearExtraidoExitoso("100/2020"))
            .ConResultado(CrearExtraidoExitoso("101/2020"));
        var handler = CrearHandler(dbContext, parser);

        var command = CrearCommand(
            dependencia.Id,
            new PdfMigrarDto(ContenidoPdfFalso, "100-2020.pdf"),
            new PdfMigrarDto(ContenidoPdfFalso, "101-2020.pdf"));

        var reporte = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(2, reporte.Exitosos);
        Assert.Equal(0, reporte.ConAdvertencia);
        Assert.Equal(0, reporte.Fallidos);
        Assert.Equal(2, reporte.TotalProcesados);

        var informes = dbContext.Informes.ToList();
        Assert.Equal(2, informes.Count);
        Assert.All(informes, informe =>
        {
            Assert.Equal(OrigenInforme.Migrado, informe.Origen);
            Assert.Null(informe.CasoAnalisisId);
            Assert.Equal(EstadoInforme.Borrador, informe.Estado);
            Assert.Equal(dependencia.Id, informe.DependenciaDestinoId);
        });
    }

    [Fact]
    public async Task MigrarInformes_LoteConPdfsExitosos_SubeElPdfOriginalAMinIOYLoAsignaAlInforme()
    {
        var (dbContext, dependencia) = await PrepararAsync();
        var parser = new FakeInformePdfParserPorArchivo().ConResultado(CrearExtraidoExitoso("100/2020"));
        var fileStorage = new FakeFileStorage();
        var handler = CrearHandler(dbContext, parser, fileStorage: fileStorage);

        var command = CrearCommand(dependencia.Id, new PdfMigrarDto(ContenidoPdfFalso, "100-2020.pdf"));

        await handler.Handle(command, CancellationToken.None);

        Assert.Single(fileStorage.ArchivosSubidos);

        var informe = Assert.Single(dbContext.Informes.ToList());
        Assert.Equal(fileStorage.ArchivosSubidos.Single(), informe.PdfPath);
    }

    [Fact]
    public async Task MigrarInformes_PdfExitosoRechazadoPorElAntivirus_CuentaComoFallidoYNoPersisteInforme()
    {
        var (dbContext, dependencia) = await PrepararAsync();
        var parser = new FakeInformePdfParserPorArchivo().ConResultado(CrearExtraidoExitoso("100/2020"));
        var antivirusScanner = new FakeAntivirusScanner { ResultadoLimpio = false };
        var handler = CrearHandler(dbContext, parser, antivirusScanner: antivirusScanner);

        var command = CrearCommand(dependencia.Id, new PdfMigrarDto(ContenidoPdfFalso, "100-2020.pdf"));

        var reporte = await handler.Handle(command, CancellationToken.None);

        Assert.Empty(dbContext.Informes.ToList());
        var detalle = reporte.Detalle.Single();
        Assert.Equal(ResultadoMigracionArchivo.Fallido, detalle.Resultado);
    }

    [Fact]
    public async Task MigrarInformes_PdfNoLegible_CuentaComoFallidoYNoAbortaElRestoDelLote()
    {
        var (dbContext, dependencia) = await PrepararAsync();
        var parser = new FakeInformePdfParserPorArchivo()
            .ConExcepcion(new InvalidOperationException("PDF corrupto"))
            .ConResultado(CrearExtraidoExitoso("102/2020"));
        var handler = CrearHandler(dbContext, parser);

        var command = CrearCommand(
            dependencia.Id,
            new PdfMigrarDto(ContenidoPdfFalso, "corrupto.pdf"),
            new PdfMigrarDto(ContenidoPdfFalso, "102-2020.pdf"));

        var reporte = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(2, reporte.TotalProcesados);
        Assert.Equal(1, reporte.Fallidos);
        Assert.Equal(1, reporte.Exitosos);
        Assert.Single(dbContext.Informes.ToList());

        var detalleFallido = reporte.Detalle.Single(d => d.NombreArchivo == "corrupto.pdf");
        Assert.Equal(ResultadoMigracionArchivo.Fallido, detalleFallido.Resultado);
        Assert.NotNull(detalleFallido.Motivo);
    }

    // A partir de HU-04, escenario "PDF con ID Registro no reconocido
    // también queda pendiente, no se pierde": cuando el ID Registro no se
    // reconoce, el PDF ya no se descarta — sigue el mismo camino que
    // "Fecha de Análisis no reconocida" (escaneo antivirus → subir a MinIO
    // → crear MigracionPendiente), pero con IdRegistro = null. El test
    // viejo "MigrarInformes_IdRegistroNoReconocido_CuentaComoAdvertenciaYNoPersiste"
    // afirmaba el comportamiento anterior (no persiste nada) — reemplazado
    // por los siguientes, que reflejan la realidad nueva.

    [Fact]
    public async Task MigrarInformes_IdRegistroNoReconocido_CuentaComoAdvertenciaYPersisteUnaMigracionPendienteConIdRegistroNulo()
    {
        var (dbContext, dependencia) = await PrepararAsync();
        var parser = new FakeInformePdfParserPorArchivo().ConResultado(CrearExtraidoSinIdRegistro());
        var handler = CrearHandler(dbContext, parser);

        var command = CrearCommand(dependencia.Id, new PdfMigrarDto(ContenidoPdfFalso, "sin-id.pdf"));

        var reporte = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(1, reporte.ConAdvertencia);
        Assert.Equal(0, reporte.Exitosos);
        Assert.Equal(0, reporte.Fallidos);
        Assert.Empty(dbContext.Informes.ToList());

        var migracionPendiente = Assert.Single(dbContext.MigracionesPendientes.ToList());
        Assert.Null(migracionPendiente.IdRegistro);
        Assert.Equal(dependencia.Id, migracionPendiente.DependenciaDestinoId);
        Assert.Equal(UsuarioMigradorId, migracionPendiente.UsuarioMigradorId);
        Assert.Equal("Relato sin ID Registro reconocido", migracionPendiente.Relato);

        var detalle = reporte.Detalle.Single();
        Assert.Equal(ResultadoMigracionArchivo.ConAdvertencia, detalle.Resultado);
        Assert.Contains("ID Registro", detalle.Motivo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MigrarInformes_IdRegistroNoReconocido_SubeElPdfOriginalAMinIO()
    {
        var (dbContext, dependencia) = await PrepararAsync();
        var parser = new FakeInformePdfParserPorArchivo().ConResultado(CrearExtraidoSinIdRegistro());
        var fileStorage = new FakeFileStorage();
        var handler = CrearHandler(dbContext, parser, fileStorage: fileStorage);

        var command = CrearCommand(dependencia.Id, new PdfMigrarDto(ContenidoPdfFalso, "sin-id.pdf"));

        await handler.Handle(command, CancellationToken.None);

        Assert.Single(fileStorage.ArchivosSubidos);

        var migracionPendiente = Assert.Single(dbContext.MigracionesPendientes.ToList());
        Assert.Equal(fileStorage.ArchivosSubidos.Single(), migracionPendiente.PdfPath);
    }

    [Fact]
    public async Task MigrarInformes_IdRegistroNoReconocido_ElArchivoRechazadoPorElAntivirusNoGeneraMigracionPendiente()
    {
        var (dbContext, dependencia) = await PrepararAsync();
        var parser = new FakeInformePdfParserPorArchivo().ConResultado(CrearExtraidoSinIdRegistro());
        var antivirusScanner = new FakeAntivirusScanner { ResultadoLimpio = false };
        var handler = CrearHandler(dbContext, parser, antivirusScanner: antivirusScanner);

        var command = CrearCommand(dependencia.Id, new PdfMigrarDto(ContenidoPdfFalso, "sin-id.pdf"));

        var reporte = await handler.Handle(command, CancellationToken.None);

        Assert.Empty(dbContext.MigracionesPendientes.ToList());
        Assert.Empty(dbContext.Informes.ToList());

        var detalle = reporte.Detalle.Single();
        Assert.Equal(ResultadoMigracionArchivo.Fallido, detalle.Resultado);
    }

    [Fact]
    public async Task MigrarInformes_VariosPdfsSinIdRegistroEnElMismoLote_CreaUnaMigracionPendienteConIdRegistroNuloPorCadaUno()
    {
        // A diferencia del chequeo de "ID Registro duplicado en el lote"
        // (que exige tener un IdRegistro concreto), acá no hay ninguna
        // clave para comparar — cada PDF sin ID Registro reconocido genera
        // su propia MigracionPendiente, sin chocar entre sí gracias al
        // índice único parcial (WHERE "IdRegistro" IS NOT NULL).
        var (dbContext, dependencia) = await PrepararAsync();
        var parser = new FakeInformePdfParserPorArchivo()
            .ConResultado(CrearExtraidoSinIdRegistro())
            .ConResultado(CrearExtraidoSinIdRegistro());
        var handler = CrearHandler(dbContext, parser);

        var command = CrearCommand(
            dependencia.Id,
            new PdfMigrarDto(ContenidoPdfFalso, "sin-id-1.pdf"),
            new PdfMigrarDto(ContenidoPdfFalso, "sin-id-2.pdf"));

        var reporte = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(2, reporte.ConAdvertencia);
        var migracionesPendientes = dbContext.MigracionesPendientes.ToList();
        Assert.Equal(2, migracionesPendientes.Count);
        Assert.All(migracionesPendientes, m => Assert.Null(m.IdRegistro));
    }

    [Fact]
    public async Task MigrarInformes_IdRegistroYaExisteEnLaBase_CuentaComoAdvertenciaYNoTocaElInformePreexistente()
    {
        var (dbContext, dependencia) = await PrepararAsync();
        var informePreexistente = Informe.CrearMigrado("200/2019", new DateOnly(2019, 5, 1), dependencia.Id, Guid.NewGuid());
        dbContext.Informes.Add(informePreexistente);
        await dbContext.SaveChangesAsync();

        var parser = new FakeInformePdfParserPorArchivo().ConResultado(CrearExtraidoExitoso("200/2019"));
        var handler = CrearHandler(dbContext, parser);

        var command = CrearCommand(dependencia.Id, new PdfMigrarDto(ContenidoPdfFalso, "200-2019.pdf"));

        var reporte = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(1, reporte.ConAdvertencia);
        Assert.Equal(0, reporte.Exitosos);
        Assert.Single(dbContext.Informes.ToList());

        var detalle = reporte.Detalle.Single();
        Assert.Equal(ResultadoMigracionArchivo.ConAdvertencia, detalle.Resultado);
        Assert.Contains("duplicado", detalle.Motivo, StringComparison.OrdinalIgnoreCase);

        var informeSinTocar = await dbContext.Informes.FindAsync(informePreexistente.Id);
        Assert.NotNull(informeSinTocar);
        Assert.Equal(informePreexistente.Id, informeSinTocar.Id);
        Assert.Equal(EstadoInforme.Borrador, informeSinTocar.Estado);
    }

    [Fact]
    public async Task MigrarInformes_IdRegistroYaExisteComoMigracionPendiente_CuentaComoAdvertenciaEnVezDeRomperElLote()
    {
        // Bug real encontrado en producción: re-migrar una carpeta que
        // incluye un PDF ya guardado como MigracionPendiente (de una
        // corrida anterior) chocaba contra el índice único de IdRegistro
        // en la base — el chequeo de duplicados solo miraba la tabla
        // Informes, no MigracionesPendientes. El DbUpdateException sin
        // capturar abortaba TODO el lote (ningún archivo de esa tanda se
        // guardaba, ni siquiera los que sí eran nuevos), no solo el
        // archivo repetido.
        var (dbContext, dependencia) = await PrepararAsync();
        var migracionPendienteExistente = new MigracionPendiente(
            "79/2022", "migraciones-pendientes/79-2022.pdf", dependencia.Id, Guid.NewGuid());
        dbContext.MigracionesPendientes.Add(migracionPendienteExistente);
        await dbContext.SaveChangesAsync();

        var parser = new FakeInformePdfParserPorArchivo()
            .ConResultado(CrearExtraidoExitoso("500/2022"))
            .ConResultado(CrearExtraidoExitoso("79/2022"));
        var handler = CrearHandler(dbContext, parser);

        var command = CrearCommand(
            dependencia.Id,
            new PdfMigrarDto(ContenidoPdfFalso, "500-2022.pdf"),
            new PdfMigrarDto(ContenidoPdfFalso, "79-2022.pdf"));

        var reporte = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(1, reporte.Exitosos);
        Assert.Equal(1, reporte.ConAdvertencia);
        Assert.Equal(0, reporte.Fallidos);

        var detalleRepetido = reporte.Detalle.Single(d => d.NombreArchivo == "79-2022.pdf");
        Assert.Equal(ResultadoMigracionArchivo.ConAdvertencia, detalleRepetido.Resultado);
        Assert.Contains("duplicado", detalleRepetido.Motivo, StringComparison.OrdinalIgnoreCase);

        // La MigracionPendiente original sigue única, sin duplicar.
        Assert.Single(dbContext.MigracionesPendientes.Where(m => m.IdRegistro == "79/2022").ToList());
    }

    [Fact]
    public async Task MigrarInformes_DosPdfsDelLoteConMismoIdRegistro_SoloElPrimeroSePersisteElSegundoEsAdvertenciaPorDuplicado()
    {
        var (dbContext, dependencia) = await PrepararAsync();
        var parser = new FakeInformePdfParserPorArchivo()
            .ConResultado(CrearExtraidoExitoso("300/2021"))
            .ConResultado(CrearExtraidoExitoso("300/2021"));
        var handler = CrearHandler(dbContext, parser);

        var command = CrearCommand(
            dependencia.Id,
            new PdfMigrarDto(ContenidoPdfFalso, "300-2021-copia1.pdf"),
            new PdfMigrarDto(ContenidoPdfFalso, "300-2021-copia2.pdf"));

        var reporte = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(1, reporte.Exitosos);
        Assert.Equal(1, reporte.ConAdvertencia);
        Assert.Single(dbContext.Informes.ToList());

        var detallePrimero = reporte.Detalle.Single(d => d.NombreArchivo == "300-2021-copia1.pdf");
        var detalleSegundo = reporte.Detalle.Single(d => d.NombreArchivo == "300-2021-copia2.pdf");
        Assert.Equal(ResultadoMigracionArchivo.Exitoso, detallePrimero.Resultado);
        Assert.Equal(ResultadoMigracionArchivo.ConAdvertencia, detalleSegundo.Resultado);
        Assert.Contains("duplicado", detalleSegundo.Motivo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MigrarInformes_LoteMixto_ElReporteSumaCorrectamenteLosConteosPorResultado()
    {
        var (dbContext, dependencia) = await PrepararAsync();
        var informePreexistente = Informe.CrearMigrado("400/2018", new DateOnly(2018, 1, 1), dependencia.Id, Guid.NewGuid());
        dbContext.Informes.Add(informePreexistente);
        await dbContext.SaveChangesAsync();

        var parser = new FakeInformePdfParserPorArchivo()
            .ConResultado(CrearExtraidoExitoso("401/2018"))       // exitoso
            .ConExcepcion(new InvalidOperationException("roto"))  // fallido
            .ConResultado(CrearExtraidoSinIdRegistro())           // advertencia: sin id
            .ConResultado(CrearExtraidoExitoso("400/2018"));      // advertencia: ya existe en base

        var handler = CrearHandler(dbContext, parser);

        var command = CrearCommand(
            dependencia.Id,
            new PdfMigrarDto(ContenidoPdfFalso, "401-2018.pdf"),
            new PdfMigrarDto(ContenidoPdfFalso, "roto.pdf"),
            new PdfMigrarDto(ContenidoPdfFalso, "sin-id.pdf"),
            new PdfMigrarDto(ContenidoPdfFalso, "400-2018-duplicado.pdf"));

        var reporte = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(4, reporte.TotalProcesados);
        Assert.Equal(1, reporte.Exitosos);
        Assert.Equal(2, reporte.ConAdvertencia);
        Assert.Equal(1, reporte.Fallidos);
        Assert.Equal(reporte.TotalProcesados, reporte.Exitosos + reporte.ConAdvertencia + reporte.Fallidos);
        Assert.Equal(4, reporte.Detalle.Count);
    }

    [Fact]
    public async Task MigrarInformes_LoteConPdfExitosoYConAdvertencia_ElReporteIncluyeElDestinoExtraidoDelPdf()
    {
        var (dbContext, dependencia) = await PrepararAsync();
        var parser = new FakeInformePdfParserPorArchivo()
            .ConResultado(CrearExtraidoExitoso("600/2023"))
            .ConResultado(CrearExtraidoSinIdRegistro() with { Destino = "Comisaría 5ta" });
        var handler = CrearHandler(dbContext, parser);

        var command = CrearCommand(
            dependencia.Id,
            new PdfMigrarDto(ContenidoPdfFalso, "600-2023.pdf"),
            new PdfMigrarDto(ContenidoPdfFalso, "sin-id.pdf"));

        var reporte = await handler.Handle(command, CancellationToken.None);

        var detalleExitoso = reporte.Detalle.Single(d => d.NombreArchivo == "600-2023.pdf");
        var detalleAdvertencia = reporte.Detalle.Single(d => d.NombreArchivo == "sin-id.pdf");

        Assert.Equal("AV. INFRACCION LEY 23.737", detalleExitoso.DestinoExtraido);
        Assert.Equal("Comisaría 5ta", detalleAdvertencia.DestinoExtraido);
    }

    [Fact]
    public async Task MigrarInformes_PdfNoLegible_ElReporteNoTraeDestinoExtraido()
    {
        var (dbContext, dependencia) = await PrepararAsync();
        var parser = new FakeInformePdfParserPorArchivo().ConExcepcion(new InvalidOperationException("PDF corrupto"));
        var handler = CrearHandler(dbContext, parser);

        var command = CrearCommand(dependencia.Id, new PdfMigrarDto(ContenidoPdfFalso, "corrupto.pdf"));

        var reporte = await handler.Handle(command, CancellationToken.None);

        Assert.Null(reporte.Detalle.Single().DestinoExtraido);
    }

    // A partir de HU-04, escenario "PDF con Fecha de Análisis no reconocida
    // queda pendiente, no se pierde": cuando el ID Registro sí se reconoce
    // pero la Fecha de Análisis no, el PDF ya no se descarta — se sube a
    // MinIO y se persiste una MigracionPendiente con los demás datos ya
    // extraídos, en vez de solo dejar un mensaje "Con advertencia" sin
    // persistir nada (comportamiento viejo, ya no vigente).

    [Fact]
    public async Task MigrarInformes_FechaAnalisisNoReconocida_CuentaComoAdvertenciaYPersisteUnaMigracionPendiente()
    {
        var (dbContext, dependencia) = await PrepararAsync();
        var extraidoSinFecha = CrearExtraidoExitoso("700/2022") with { FechaAnalisis = null };
        var parser = new FakeInformePdfParserPorArchivo().ConResultado(extraidoSinFecha);
        var handler = CrearHandler(dbContext, parser);

        var command = CrearCommand(dependencia.Id, new PdfMigrarDto(ContenidoPdfFalso, "700-2022.pdf"));

        var reporte = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(1, reporte.ConAdvertencia);
        Assert.Equal(0, reporte.Exitosos);
        Assert.Equal(0, reporte.Fallidos);

        // No se descarta: no crea el Informe todavía (falta la fecha), pero
        // tampoco se pierde el PDF ni los datos ya extraídos.
        Assert.Empty(dbContext.Informes.ToList());

        var migracionesPendientes = dbContext.MigracionesPendientes.ToList();
        var migracionPendiente = Assert.Single(migracionesPendientes);
        Assert.Equal("700/2022", migracionPendiente.IdRegistro);
        Assert.Equal(dependencia.Id, migracionPendiente.DependenciaDestinoId);
        Assert.Equal(UsuarioMigradorId, migracionPendiente.UsuarioMigradorId);
        Assert.Equal("Se procede a realizar el análisis histórico...", migracionPendiente.Relato);

        var detalle = reporte.Detalle.Single();
        Assert.Equal(ResultadoMigracionArchivo.ConAdvertencia, detalle.Resultado);
        Assert.Contains("Fecha", detalle.Motivo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MigrarInformes_FechaAnalisisNoReconocida_SubeElPdfOriginalAMinIO()
    {
        var (dbContext, dependencia) = await PrepararAsync();
        var extraidoSinFecha = CrearExtraidoExitoso("701/2022") with { FechaAnalisis = null };
        var parser = new FakeInformePdfParserPorArchivo().ConResultado(extraidoSinFecha);
        var fileStorage = new FakeFileStorage();
        var handler = CrearHandler(dbContext, parser, fileStorage: fileStorage);

        var command = CrearCommand(dependencia.Id, new PdfMigrarDto(ContenidoPdfFalso, "701-2022.pdf"));

        await handler.Handle(command, CancellationToken.None);

        Assert.Single(fileStorage.ArchivosSubidos);

        var migracionPendiente = Assert.Single(dbContext.MigracionesPendientes.ToList());
        Assert.Equal(fileStorage.ArchivosSubidos.Single(), migracionPendiente.PdfPath);
    }

    [Fact]
    public async Task MigrarInformes_FechaAnalisisNoReconocida_ElArchivoRechazadoPorElAntivirusNoGeneraMigracionPendiente()
    {
        var (dbContext, dependencia) = await PrepararAsync();
        var extraidoSinFecha = CrearExtraidoExitoso("702/2022") with { FechaAnalisis = null };
        var parser = new FakeInformePdfParserPorArchivo().ConResultado(extraidoSinFecha);
        var antivirusScanner = new FakeAntivirusScanner { ResultadoLimpio = false };
        var handler = CrearHandler(dbContext, parser, antivirusScanner: antivirusScanner);

        var command = CrearCommand(dependencia.Id, new PdfMigrarDto(ContenidoPdfFalso, "702-2022.pdf"));

        var reporte = await handler.Handle(command, CancellationToken.None);

        Assert.Empty(dbContext.MigracionesPendientes.ToList());
        Assert.Empty(dbContext.Informes.ToList());

        var detalle = reporte.Detalle.Single();
        Assert.Equal(ResultadoMigracionArchivo.Fallido, detalle.Resultado);
    }

    [Fact]
    public async Task MigrarInformes_IdRegistroReconocidoYFechaReconocida_NoPersisteMigracionPendiente()
    {
        // Distingue explícitamente los "Con advertencia" de un caso
        // Exitoso: a partir de HU-04 (extensión) los dos "Con advertencia"
        // (falta ID Registro, falta Fecha de Análisis) crean una
        // MigracionPendiente — solo un PDF completo y reconocido, o uno
        // Fallido (no legible), no genera ninguna.
        var (dbContext, dependencia) = await PrepararAsync();
        var parser = new FakeInformePdfParserPorArchivo().ConResultado(CrearExtraidoExitoso("510/2022"));
        var handler = CrearHandler(dbContext, parser);

        var command = CrearCommand(dependencia.Id, new PdfMigrarDto(ContenidoPdfFalso, "510-2022.pdf"));

        await handler.Handle(command, CancellationToken.None);

        Assert.Empty(dbContext.MigracionesPendientes.ToList());
    }

    [Fact]
    public async Task MigrarInformes_LoteMixtoConAdvertenciaPorFecha_ElReporteMarcaElArchivoConAdvertenciaYQuedaUnaSolaMigracionPendienteParaCompletar()
    {
        // El Gherkin exige que el reporte "marque 'Con advertencia' con un
        // enlace para completarlo" — la pantalla /informes/migrar/pendientes
        // (HU-04) arma ese enlace listando las MigracionPendiente
        // existentes, no necesita un campo extra en el reporte del lote:
        // alcanza con que quede exactamente una MigracionPendiente
        // localizable por su ID Registro.
        var (dbContext, dependencia) = await PrepararAsync();
        var extraidoSinFecha = CrearExtraidoExitoso("703/2022") with { FechaAnalisis = null };
        var parser = new FakeInformePdfParserPorArchivo()
            .ConResultado(CrearExtraidoExitoso("704/2022"))
            .ConResultado(extraidoSinFecha);
        var handler = CrearHandler(dbContext, parser);

        var command = CrearCommand(
            dependencia.Id,
            new PdfMigrarDto(ContenidoPdfFalso, "704-2022.pdf"),
            new PdfMigrarDto(ContenidoPdfFalso, "703-2022.pdf"));

        var reporte = await handler.Handle(command, CancellationToken.None);

        var detalleConAdvertencia = reporte.Detalle.Single(d => d.NombreArchivo == "703-2022.pdf");
        Assert.Equal(ResultadoMigracionArchivo.ConAdvertencia, detalleConAdvertencia.Resultado);

        var migracionPendiente = Assert.Single(dbContext.MigracionesPendientes.ToList());
        Assert.Equal("703/2022", migracionPendiente.IdRegistro);
    }

    [Fact]
    public async Task MigrarInformes_PdfQueTardaMasQueElTimeout_CuentaComoFallidoYSigueConElRestoDelLote()
    {
        var (dbContext, dependencia) = await PrepararAsync();
        var parser = new FakeInformePdfParserPorArchivo()
            .ConDemora(TimeSpan.FromSeconds(5), CrearExtraidoExitoso("999/2022"))
            .ConResultado(CrearExtraidoExitoso("998/2022"));
        var handler = CrearHandler(dbContext, parser, timeoutPorArchivo: TimeSpan.FromSeconds(1));

        var command = CrearCommand(
            dependencia.Id,
            new PdfMigrarDto(ContenidoPdfFalso, "lento.pdf"),
            new PdfMigrarDto(ContenidoPdfFalso, "998-2022.pdf"));

        var reporte = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(2, reporte.TotalProcesados);
        Assert.Equal(1, reporte.Fallidos);
        Assert.Equal(1, reporte.Exitosos);

        var detalleLento = reporte.Detalle.Single(d => d.NombreArchivo == "lento.pdf");
        Assert.Equal(ResultadoMigracionArchivo.Fallido, detalleLento.Resultado);
        Assert.Contains("tardó demasiado", detalleLento.Motivo, StringComparison.OrdinalIgnoreCase);

        var informes = dbContext.Informes.ToList();
        Assert.Single(informes);
        Assert.Equal("998/2022", informes[0].IdRegistro);
    }

    [Fact]
    public async Task MigrarInformes_DependenciaDestinoInexistente_RechazaConEntidadNoEncontrada()
    {
        var dbContext = new TestAppDbContext();
        var parser = new FakeInformePdfParserPorArchivo().ConResultado(CrearExtraidoExitoso("500/2022"));
        var handler = CrearHandler(dbContext, parser);

        var command = CrearCommand(Guid.NewGuid(), new PdfMigrarDto(ContenidoPdfFalso, "500-2022.pdf"));

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public void MigrarInformesCommand_DeclaraAutorizacion_SoloParaRolAdmin()
    {
        var atributo = typeof(MigrarInformesCommand)
            .GetCustomAttributes(typeof(AutorizarAttribute), inherit: true)
            .Cast<AutorizarAttribute>()
            .SingleOrDefault();

        Assert.NotNull(atributo);
        Assert.Single(atributo.Roles);
        Assert.Equal(Roles.Admin, atributo.Roles.Single());
    }

    [Fact]
    public async Task MigrarInformes_RegistraElIntentoDeMigracionEnAuditLog_IndependienteDelResultado()
    {
        var (dbContext, dependencia) = await PrepararAsync();
        var parser = new FakeInformePdfParserPorArchivo().ConResultado(CrearExtraidoSinIdRegistro());
        var auditLogger = new FakeAuditLogger();
        var handler = CrearHandler(dbContext, parser, auditLogger: auditLogger);

        var command = CrearCommand(dependencia.Id, new PdfMigrarDto(ContenidoPdfFalso, "sin-id.pdf"));

        await handler.Handle(command, CancellationToken.None);

        Assert.Contains(auditLogger.Registros, r => r.Accion == "MigracionLote" && r.Entidad == nameof(Informe));
    }
}
