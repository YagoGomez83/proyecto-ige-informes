using IGE.Informes.Application.Informes.Queries.ListarInformesPaginado;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Informes.ListarInformesPaginado;

/// <summary>
/// Tests de los escenarios Gherkin de "Listado de Informes: orden,
/// Causa/Dependencia visibles y filtro 'sin Causa'" (extensión de HU-01,
/// docs/epic-01-gestion-informes.md). El Query/DTO/Handler extendidos
/// todavía no existen — estos tests deben fallar en rojo (por tipos
/// faltantes: OrdenDireccion, InformeListadoDto, y la nueva firma de
/// ListarInformesPaginadoQuery/Handler) hasta que se implementen (TDD),
/// ver .claude/agents/gherkin-test-writer.md.
///
/// Firma asumida (ver notas de modelado en el epic):
///   ListarInformesPaginadoQuery(int Pagina = 1, int TamanioPagina = 50,
///     OrdenDireccion OrdenDireccion = OrdenDireccion.Desc,
///     bool SoloSinCausa = false) : IRequest&lt;PagedResult&lt;InformeListadoDto&gt;&gt;
///   enum OrdenDireccion { Asc, Desc }
///   InformeListadoDto(Guid Id, string IdRegistro, DateOnly FechaAnalisis,
///     string DependenciaNombre, string? CausaCaratula, EstadoInforme Estado)
/// </summary>
public class ListarInformesPaginadoQueryHandlerTests
{
    [Fact]
    public async Task ListarInformesPaginado_SinEspecificarOrden_DebeOrdenarPorFechaAnalisisDescendente()
    {
        var dbContext = new TestAppDbContext();
        var dependencia = new Dependencia("Departamento Investigaciones", TipoDependencia.Division);
        dbContext.Dependencias.Add(dependencia);
        await dbContext.SaveChangesAsync();

        var informeAntiguo = Informe.CrearMigrado(
            "100/2025",
            new DateOnly(2025, 1, 10),
            dependencia.Id,
            Guid.NewGuid());
        var informeReciente = Informe.CrearMigrado(
            "200/2026",
            new DateOnly(2026, 6, 1),
            dependencia.Id,
            Guid.NewGuid());
        var informeIntermedio = Informe.CrearMigrado(
            "150/2025",
            new DateOnly(2025, 8, 20),
            dependencia.Id,
            Guid.NewGuid());
        dbContext.Informes.AddRange(informeAntiguo, informeReciente, informeIntermedio);
        await dbContext.SaveChangesAsync();

        var handler = new ListarInformesPaginadoQueryHandler(dbContext, new FakeAuditLogger());

        var resultado = await handler.Handle(new ListarInformesPaginadoQuery(), CancellationToken.None);

        Assert.Equal(
            [informeReciente.Id, informeIntermedio.Id, informeAntiguo.Id],
            resultado.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task ListarInformesPaginado_OrdenAscendente_DebeOrdenarPorFechaAnalisisAscendente()
    {
        var dbContext = new TestAppDbContext();
        var dependencia = new Dependencia("Departamento Investigaciones", TipoDependencia.Division);
        dbContext.Dependencias.Add(dependencia);
        await dbContext.SaveChangesAsync();

        var informeAntiguo = Informe.CrearMigrado(
            "100/2025",
            new DateOnly(2025, 1, 10),
            dependencia.Id,
            Guid.NewGuid());
        var informeReciente = Informe.CrearMigrado(
            "200/2026",
            new DateOnly(2026, 6, 1),
            dependencia.Id,
            Guid.NewGuid());
        dbContext.Informes.AddRange(informeAntiguo, informeReciente);
        await dbContext.SaveChangesAsync();

        var handler = new ListarInformesPaginadoQueryHandler(dbContext, new FakeAuditLogger());

        var resultado = await handler.Handle(
            new ListarInformesPaginadoQuery(OrdenDireccion: OrdenDireccion.Asc),
            CancellationToken.None);

        Assert.Equal(
            [informeAntiguo.Id, informeReciente.Id],
            resultado.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task ListarInformesPaginado_InformeConCausaVinculada_DebeMostrarCaratulaYDependencia()
    {
        var dbContext = new TestAppDbContext();
        var dependencia = new Dependencia("Comisaría Seccional Primera", TipoDependencia.Comisaria);
        dbContext.Dependencias.Add(dependencia);
        var causa = new Causa("N.N. s/ Hurto", "123/2026", "Primera Circunscripción");
        dbContext.Causas.Add(causa);
        await dbContext.SaveChangesAsync();

        var informe = Informe.CrearMigrado(
            "300/2026",
            new DateOnly(2026, 3, 15),
            dependencia.Id,
            Guid.NewGuid());
        informe.AsignarCausa(causa.Id);
        dbContext.Informes.Add(informe);
        await dbContext.SaveChangesAsync();

        var handler = new ListarInformesPaginadoQueryHandler(dbContext, new FakeAuditLogger());

        var resultado = await handler.Handle(new ListarInformesPaginadoQuery(), CancellationToken.None);

        var item = Assert.Single(resultado.Items);
        Assert.Equal(dependencia.Nombre, item.DependenciaNombre);
        Assert.Equal(causa.Caratula, item.CausaCaratula);
    }

    [Fact]
    public async Task ListarInformesPaginado_InformeSinCausaVinculada_DebeTraerCausaCaratulaNull()
    {
        var dbContext = new TestAppDbContext();
        var dependencia = new Dependencia("Comisaría Seccional Segunda", TipoDependencia.Comisaria);
        dbContext.Dependencias.Add(dependencia);
        await dbContext.SaveChangesAsync();

        var informeMigrado = Informe.CrearMigrado(
            "400/2026",
            new DateOnly(2026, 4, 1),
            dependencia.Id,
            Guid.NewGuid());
        dbContext.Informes.Add(informeMigrado);
        await dbContext.SaveChangesAsync();

        var handler = new ListarInformesPaginadoQueryHandler(dbContext, new FakeAuditLogger());

        var resultado = await handler.Handle(new ListarInformesPaginadoQuery(), CancellationToken.None);

        var item = Assert.Single(resultado.Items);
        Assert.Null(item.CausaCaratula);
        Assert.Equal(dependencia.Nombre, item.DependenciaNombre);
    }

    [Fact]
    public async Task ListarInformesPaginado_DependenciaDestinoHuerfana_DebeMostrarInformeIgual()
    {
        var dbContext = new TestAppDbContext();
        var dependenciaId = Guid.NewGuid();

        var informe = Informe.CrearMigrado(
            "700/2026",
            new DateOnly(2026, 7, 1),
            dependenciaId,
            Guid.NewGuid());
        dbContext.Informes.Add(informe);
        await dbContext.SaveChangesAsync();

        var handler = new ListarInformesPaginadoQueryHandler(dbContext, new FakeAuditLogger());

        var resultado = await handler.Handle(new ListarInformesPaginadoQuery(), CancellationToken.None);

        var item = Assert.Single(resultado.Items);
        Assert.Equal(informe.Id, item.Id);
        Assert.Equal("Dependencia no encontrada", item.DependenciaNombre);
    }

    [Fact]
    public async Task ListarInformesPaginado_SoloSinCausaActivado_DebeTraerUnicamenteInformesSinCausa()
    {
        var dbContext = new TestAppDbContext();
        var dependencia = new Dependencia("Comisaría Seccional Tercera", TipoDependencia.Comisaria);
        dbContext.Dependencias.Add(dependencia);
        var causa = new Causa("N.N. s/ Robo", "456/2026", null);
        dbContext.Causas.Add(causa);
        await dbContext.SaveChangesAsync();

        var informeConCausa = Informe.CrearMigrado(
            "500/2026",
            new DateOnly(2026, 5, 1),
            dependencia.Id,
            Guid.NewGuid());
        informeConCausa.AsignarCausa(causa.Id);

        var informeSinCausa = Informe.CrearMigrado(
            "501/2026",
            new DateOnly(2026, 5, 2),
            dependencia.Id,
            Guid.NewGuid());

        dbContext.Informes.AddRange(informeConCausa, informeSinCausa);
        await dbContext.SaveChangesAsync();

        var handler = new ListarInformesPaginadoQueryHandler(dbContext, new FakeAuditLogger());

        var resultado = await handler.Handle(
            new ListarInformesPaginadoQuery(SoloSinCausa: true),
            CancellationToken.None);

        var item = Assert.Single(resultado.Items);
        Assert.Equal(informeSinCausa.Id, item.Id);
        Assert.Null(item.CausaCaratula);
    }

    [Fact]
    public async Task ListarInformesPaginado_SoloSinCausaDesactivado_DebeTraerTodosLosInformes()
    {
        var dbContext = new TestAppDbContext();
        var dependencia = new Dependencia("Comisaría Seccional Cuarta", TipoDependencia.Comisaria);
        dbContext.Dependencias.Add(dependencia);
        var causa = new Causa("N.N. s/ Daños", "789/2026", null);
        dbContext.Causas.Add(causa);
        await dbContext.SaveChangesAsync();

        var informeConCausa = Informe.CrearMigrado(
            "600/2026",
            new DateOnly(2026, 6, 1),
            dependencia.Id,
            Guid.NewGuid());
        informeConCausa.AsignarCausa(causa.Id);

        var informeSinCausa = Informe.CrearMigrado(
            "601/2026",
            new DateOnly(2026, 6, 2),
            dependencia.Id,
            Guid.NewGuid());

        dbContext.Informes.AddRange(informeConCausa, informeSinCausa);
        await dbContext.SaveChangesAsync();

        var handler = new ListarInformesPaginadoQueryHandler(dbContext, new FakeAuditLogger());

        var resultado = await handler.Handle(
            new ListarInformesPaginadoQuery(SoloSinCausa: false),
            CancellationToken.None);

        Assert.Equal(2, resultado.Items.Count);
        Assert.Contains(resultado.Items, i => i.Id == informeConCausa.Id);
        Assert.Contains(resultado.Items, i => i.Id == informeSinCausa.Id);
    }
}
