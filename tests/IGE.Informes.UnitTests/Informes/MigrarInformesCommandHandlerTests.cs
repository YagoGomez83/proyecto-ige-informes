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
/// escenario "Migración por lote". El Command/Handler todavía no existen:
/// estos tests están escritos antes de la implementación (TDD), por lo que
/// deben fallar en rojo hasta que se cree
/// IGE.Informes.Application.Informes.Commands.MigrarInformes.*.
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
        var handler = new MigrarInformesCommandHandler(dbContext, new FakeCurrentUserService(UsuarioMigradorId, Roles.Admin), parser, new FakeAuditLogger());

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
    public async Task MigrarInformes_PdfNoLegible_CuentaComoFallidoYNoAbortaElRestoDelLote()
    {
        var (dbContext, dependencia) = await PrepararAsync();
        var parser = new FakeInformePdfParserPorArchivo()
            .ConExcepcion(new InvalidOperationException("PDF corrupto"))
            .ConResultado(CrearExtraidoExitoso("102/2020"));
        var handler = new MigrarInformesCommandHandler(dbContext, new FakeCurrentUserService(UsuarioMigradorId, Roles.Admin), parser, new FakeAuditLogger());

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

    [Fact]
    public async Task MigrarInformes_IdRegistroNoReconocido_CuentaComoAdvertenciaYNoPersiste()
    {
        var (dbContext, dependencia) = await PrepararAsync();
        var parser = new FakeInformePdfParserPorArchivo().ConResultado(CrearExtraidoSinIdRegistro());
        var handler = new MigrarInformesCommandHandler(dbContext, new FakeCurrentUserService(UsuarioMigradorId, Roles.Admin), parser, new FakeAuditLogger());

        var command = CrearCommand(dependencia.Id, new PdfMigrarDto(ContenidoPdfFalso, "sin-id.pdf"));

        var reporte = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(1, reporte.ConAdvertencia);
        Assert.Equal(0, reporte.Exitosos);
        Assert.Equal(0, reporte.Fallidos);
        Assert.Empty(dbContext.Informes.ToList());

        var detalle = reporte.Detalle.Single();
        Assert.Equal(ResultadoMigracionArchivo.ConAdvertencia, detalle.Resultado);
        Assert.Contains("ID Registro", detalle.Motivo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MigrarInformes_IdRegistroYaExisteEnLaBase_CuentaComoAdvertenciaYNoTocaElInformePreexistente()
    {
        var (dbContext, dependencia) = await PrepararAsync();
        var informePreexistente = Informe.CrearMigrado("200/2019", new DateOnly(2019, 5, 1), dependencia.Id, Guid.NewGuid());
        dbContext.Informes.Add(informePreexistente);
        await dbContext.SaveChangesAsync();

        var parser = new FakeInformePdfParserPorArchivo().ConResultado(CrearExtraidoExitoso("200/2019"));
        var handler = new MigrarInformesCommandHandler(dbContext, new FakeCurrentUserService(UsuarioMigradorId, Roles.Admin), parser, new FakeAuditLogger());

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
    public async Task MigrarInformes_DosPdfsDelLoteConMismoIdRegistro_SoloElPrimeroSePersisteElSegundoEsAdvertenciaPorDuplicado()
    {
        var (dbContext, dependencia) = await PrepararAsync();
        var parser = new FakeInformePdfParserPorArchivo()
            .ConResultado(CrearExtraidoExitoso("300/2021"))
            .ConResultado(CrearExtraidoExitoso("300/2021"));
        var handler = new MigrarInformesCommandHandler(dbContext, new FakeCurrentUserService(UsuarioMigradorId, Roles.Admin), parser, new FakeAuditLogger());

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

        var handler = new MigrarInformesCommandHandler(dbContext, new FakeCurrentUserService(UsuarioMigradorId, Roles.Admin), parser, new FakeAuditLogger());

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
        var handler = new MigrarInformesCommandHandler(dbContext, new FakeCurrentUserService(UsuarioMigradorId, Roles.Admin), parser, new FakeAuditLogger());

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
        var handler = new MigrarInformesCommandHandler(dbContext, new FakeCurrentUserService(UsuarioMigradorId, Roles.Admin), parser, new FakeAuditLogger());

        var command = CrearCommand(dependencia.Id, new PdfMigrarDto(ContenidoPdfFalso, "corrupto.pdf"));

        var reporte = await handler.Handle(command, CancellationToken.None);

        Assert.Null(reporte.Detalle.Single().DestinoExtraido);
    }

    [Fact]
    public async Task MigrarInformes_DependenciaDestinoInexistente_RechazaConEntidadNoEncontrada()
    {
        var dbContext = new TestAppDbContext();
        var parser = new FakeInformePdfParserPorArchivo().ConResultado(CrearExtraidoExitoso("500/2022"));
        var handler = new MigrarInformesCommandHandler(dbContext, new FakeCurrentUserService(UsuarioMigradorId, Roles.Admin), parser, new FakeAuditLogger());

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
        var handler = new MigrarInformesCommandHandler(dbContext, new FakeCurrentUserService(UsuarioMigradorId, Roles.Admin), parser, auditLogger);

        var command = CrearCommand(dependencia.Id, new PdfMigrarDto(ContenidoPdfFalso, "sin-id.pdf"));

        await handler.Handle(command, CancellationToken.None);

        Assert.Contains(auditLogger.Registros, r => r.Accion == "MigracionLote" && r.Entidad == nameof(Informe));
    }
}
