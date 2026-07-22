using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Application.Informes.Queries.ParsearPdfInforme;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Informes;

public class ParsearPdfInformeQueryHandlerTests
{
    [Fact]
    public async Task Devuelve_los_datos_extraidos_sin_persistir_nada()
    {
        var dbContext = new TestAppDbContext();
        var extraido = new InformeExtraidoDto(
            "290/2026", new DateOnly(2026, 7, 14), "AV. INFRACCION LEY 23.737", "DIVISION LUCHA CONTRA EL NARCO", "7070029/26",
            "Relato de prueba", [], [], []);

        var handler = new ParsearPdfInformeQueryHandler(dbContext, new FakeInformePdfParser(extraido), new FakeAuditLogger());

        var resultado = await handler.Handle(new ParsearPdfInformeQuery([1, 2, 3]), CancellationToken.None);

        Assert.Equal("290/2026", resultado.IdRegistro);
        Assert.False(resultado.YaExisteInformeConEseIdRegistro);
        Assert.False(resultado.RequiereRevisionManual);
        Assert.Empty(dbContext.Informes); // no persistió nada
    }

    [Fact]
    public async Task Marca_YaExisteInformeConEseIdRegistro_si_el_IdRegistro_ya_esta_en_uso()
    {
        var dbContext = new TestAppDbContext();
        var informeExistente = new Informe("290/2026", new DateOnly(2026, 7, 14), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        dbContext.Informes.Add(informeExistente);
        await dbContext.SaveChangesAsync();

        var extraido = new InformeExtraidoDto("290/2026", null, null, null, null, null, [], [], []);
        var handler = new ParsearPdfInformeQueryHandler(dbContext, new FakeInformePdfParser(extraido), new FakeAuditLogger());

        var resultado = await handler.Handle(new ParsearPdfInformeQuery([1, 2, 3]), CancellationToken.None);

        Assert.True(resultado.YaExisteInformeConEseIdRegistro);
    }

    [Fact]
    public async Task Sin_IdRegistro_reconocido_marca_RequiereRevisionManual()
    {
        var dbContext = new TestAppDbContext();
        var extraido = new InformeExtraidoDto(null, null, null, null, null, null, [], [], []);
        var handler = new ParsearPdfInformeQueryHandler(dbContext, new FakeInformePdfParser(extraido), new FakeAuditLogger());

        var resultado = await handler.Handle(new ParsearPdfInformeQuery([1, 2, 3]), CancellationToken.None);

        Assert.True(resultado.RequiereRevisionManual);
        Assert.False(resultado.YaExisteInformeConEseIdRegistro);
    }

    [Fact]
    public async Task Registra_auditoria_de_extraccion_cuando_hay_DNIs_extraidos()
    {
        var dbContext = new TestAppDbContext();
        var extraido = new InformeExtraidoDto(
            "290/2026", null, null, null, null, null, [],
            [new PersonaExtraidaDto("30123456", "Denunciante")], []);

        var auditLogger = new FakeAuditLogger();
        var handler = new ParsearPdfInformeQueryHandler(dbContext, new FakeInformePdfParser(extraido), auditLogger);

        await handler.Handle(new ParsearPdfInformeQuery([1, 2, 3]), CancellationToken.None);

        Assert.Single(auditLogger.Registros);
        Assert.Equal(("ExtraccionPdf", nameof(Persona), (Guid?)null), auditLogger.Registros[0]);
    }

    [Fact]
    public async Task No_registra_auditoria_si_no_se_extrajeron_personas()
    {
        var dbContext = new TestAppDbContext();
        var extraido = new InformeExtraidoDto("290/2026", null, null, null, null, null, [], [], []);

        var auditLogger = new FakeAuditLogger();
        var handler = new ParsearPdfInformeQueryHandler(dbContext, new FakeInformePdfParser(extraido), auditLogger);

        await handler.Handle(new ParsearPdfInformeQuery([1, 2, 3]), CancellationToken.None);

        Assert.Empty(auditLogger.Registros);
    }

    [Fact]
    public async Task Resuelve_CamaraId_cuando_el_codigo_de_camara_existe()
    {
        var dbContext = new TestAppDbContext();
        var camara = new Camara("SL 18", TipoCamara.Domo, "Av. Illia y San Martín");
        dbContext.Camaras.Add(camara);
        await dbContext.SaveChangesAsync();

        var extraido = new InformeExtraidoDto(
            "501/2026", null, null, null, null, null, [], [],
            [new EvidenciaExtraidaDto(1, "SL 18", "Av. Illia y San Martín", null, "Descripción")]);

        var handler = new ParsearPdfInformeQueryHandler(dbContext, new FakeInformePdfParser(extraido), new FakeAuditLogger());

        var resultado = await handler.Handle(new ParsearPdfInformeQuery([1, 2, 3]), CancellationToken.None);

        Assert.Equal(camara.Id, resultado.Evidencias.Single().CamaraId);
    }

    [Fact]
    public async Task No_resuelve_CamaraId_cuando_el_codigo_no_matchea_ninguna_camara()
    {
        var dbContext = new TestAppDbContext();
        var extraido = new InformeExtraidoDto(
            "501/2026", null, null, null, null, null, [], [],
            [new EvidenciaExtraidaDto(1, "CAM DESCONOCIDA", null, null, "Descripción")]);

        var handler = new ParsearPdfInformeQueryHandler(dbContext, new FakeInformePdfParser(extraido), new FakeAuditLogger());

        var resultado = await handler.Handle(new ParsearPdfInformeQuery([1, 2, 3]), CancellationToken.None);

        Assert.Null(resultado.Evidencias.Single().CamaraId);
    }
}
