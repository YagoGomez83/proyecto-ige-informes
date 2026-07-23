using IGE.Informes.Application.CasosAnalisis.Queries.ObtenerTableroAnalitica;
using IGE.Informes.Application.Common.Security;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.CasosAnalisis;

/// <summary>
/// HU-06 · Tablero de analítica de gestión (Épica 02). El tablero cuenta
/// CasoAnalisis (no Informe) — ver nota de decisión en
/// docs/epic-02-busqueda-analitica.md: Dependencia = jurisdicción del
/// llamado (CasoAnalisis.DependenciaId), TipoIncidente =
/// CasoAnalisis.TipoIncidenteId, Analista = vía CasoAnalista (cualquier rol,
/// Operador o Creador), Resultado = CasoAnalisis.Resultado (nullable).
///
/// Todavía NO existe ObtenerTableroAnaliticaQuery/Handler en producción —
/// estos tests son la especificación ejecutable (deben fallar en rojo por
/// ausencia de implementación / error de compilación) hasta que se
/// implemente el Handler.
/// </summary>
public class ObtenerTableroAnaliticaQueryHandlerTests
{
    private static CasoAnalisis CrearCaso(
        DateOnly fecha,
        Guid dependenciaId,
        Guid tipoIncidenteId,
        Guid creadorUsuarioId,
        ResultadoCaso? resultado = null)
    {
        var caso = new CasoAnalisis(fecha, dependenciaId, tipoIncidenteId, creadorUsuarioId);

        if (resultado is { } r)
        {
            caso.CerrarConResultado(r);
        }

        return caso;
    }

    [Fact]
    public async Task ObtenerTableroAnalitica_ReportePorDependencia_DevuelveConteoDeCasosPorCadaDependenciaEnElRango()
    {
        // Escenario Gherkin: "Reporte por dependencia"
        var dbContext = new TestAppDbContext();
        var dependenciaA = Guid.NewGuid();
        var dependenciaB = Guid.NewGuid();
        var tipoIncidente = Guid.NewGuid();
        var creador = Guid.NewGuid();

        dbContext.CasosAnalisis.Add(CrearCaso(new DateOnly(2026, 1, 10), dependenciaA, tipoIncidente, creador));
        dbContext.CasosAnalisis.Add(CrearCaso(new DateOnly(2026, 1, 11), dependenciaA, tipoIncidente, creador));
        dbContext.CasosAnalisis.Add(CrearCaso(new DateOnly(2026, 1, 12), dependenciaB, tipoIncidente, creador));
        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new ObtenerTableroAnaliticaQueryHandler(dbContext, auditLogger);

        var resultado = await handler.Handle(new ObtenerTableroAnaliticaQuery(), CancellationToken.None);

        Assert.Equal(2, resultado.PorDependencia.Count);
        Assert.Contains(resultado.PorDependencia, c => c.DependenciaId == dependenciaA && c.Cantidad == 2);
        Assert.Contains(resultado.PorDependencia, c => c.DependenciaId == dependenciaB && c.Cantidad == 1);
    }

    [Fact]
    public async Task ObtenerTableroAnalitica_ReportePorTipoIncidente_DevuelveConteoDeCasosPorCadaTipoIncidenteEnElRango()
    {
        // Escenario Gherkin: "Reporte por tipo de incidente"
        var dbContext = new TestAppDbContext();
        var dependencia = Guid.NewGuid();
        var tipoRobo = Guid.NewGuid();
        var tipoHurto = Guid.NewGuid();
        var creador = Guid.NewGuid();

        dbContext.CasosAnalisis.Add(CrearCaso(new DateOnly(2026, 2, 1), dependencia, tipoRobo, creador));
        dbContext.CasosAnalisis.Add(CrearCaso(new DateOnly(2026, 2, 2), dependencia, tipoRobo, creador));
        dbContext.CasosAnalisis.Add(CrearCaso(new DateOnly(2026, 2, 3), dependencia, tipoRobo, creador));
        dbContext.CasosAnalisis.Add(CrearCaso(new DateOnly(2026, 2, 4), dependencia, tipoHurto, creador));
        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new ObtenerTableroAnaliticaQueryHandler(dbContext, auditLogger);

        var resultado = await handler.Handle(new ObtenerTableroAnaliticaQuery(), CancellationToken.None);

        Assert.Equal(2, resultado.PorTipoIncidente.Count);
        Assert.Contains(resultado.PorTipoIncidente, c => c.TipoIncidenteId == tipoRobo && c.Cantidad == 3);
        Assert.Contains(resultado.PorTipoIncidente, c => c.TipoIncidenteId == tipoHurto && c.Cantidad == 1);
    }

    [Fact]
    public async Task ObtenerTableroAnalitica_ReportePorAnalista_CuentaUnaVezPorCasoAAmbosRolesOperadorYCreador()
    {
        // Escenario Gherkin: "Reporte por analista". Un Caso puede tener
        // varios analistas asignados (Creador + Operador/es, ver
        // CasoAnalista) — cada analista suma 1 al conteo de cada Caso donde
        // participa, sin importar el rol.
        var dbContext = new TestAppDbContext();
        var dependencia = Guid.NewGuid();
        var tipoIncidente = Guid.NewGuid();
        var analistaCreador = Guid.NewGuid();
        var analistaOperador = Guid.NewGuid();

        var casoConDosAnalistas = CrearCaso(new DateOnly(2026, 3, 1), dependencia, tipoIncidente, analistaCreador);
        casoConDosAnalistas.AsignarAnalista(analistaOperador, RolCasoAnalista.Operador);

        var casoSoloConCreador = CrearCaso(new DateOnly(2026, 3, 2), dependencia, tipoIncidente, analistaCreador);

        dbContext.CasosAnalisis.Add(casoConDosAnalistas);
        dbContext.CasosAnalisis.Add(casoSoloConCreador);
        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new ObtenerTableroAnaliticaQueryHandler(dbContext, auditLogger);

        var resultado = await handler.Handle(new ObtenerTableroAnaliticaQuery(), CancellationToken.None);

        Assert.Equal(2, resultado.PorAnalista.Count);
        Assert.Contains(resultado.PorAnalista, c => c.AnalistaUsuarioId == analistaCreador && c.Cantidad == 2);
        Assert.Contains(resultado.PorAnalista, c => c.AnalistaUsuarioId == analistaOperador && c.Cantidad == 1);
    }

    [Fact]
    public async Task ObtenerTableroAnalitica_ReportePorResultado_DevuelveConteoDeCasosPorResultadoYExcluyeSinResultado()
    {
        // Escenario Gherkin: "Reporte por resultado" (Positivo, Negativo,
        // Revisión). Un Caso Pendiente (Resultado == null) todavía no tiene
        // un análisis terminado — no debe contarse en ninguna de las 3
        // categorías del reporte por resultado.
        var dbContext = new TestAppDbContext();
        var dependencia = Guid.NewGuid();
        var tipoIncidente = Guid.NewGuid();
        var creador = Guid.NewGuid();

        dbContext.CasosAnalisis.Add(CrearCaso(new DateOnly(2026, 4, 1), dependencia, tipoIncidente, creador, ResultadoCaso.Positivo));
        dbContext.CasosAnalisis.Add(CrearCaso(new DateOnly(2026, 4, 2), dependencia, tipoIncidente, creador, ResultadoCaso.Positivo));
        dbContext.CasosAnalisis.Add(CrearCaso(new DateOnly(2026, 4, 3), dependencia, tipoIncidente, creador, ResultadoCaso.Negativo));
        dbContext.CasosAnalisis.Add(CrearCaso(new DateOnly(2026, 4, 4), dependencia, tipoIncidente, creador, ResultadoCaso.Revision));
        dbContext.CasosAnalisis.Add(CrearCaso(new DateOnly(2026, 4, 5), dependencia, tipoIncidente, creador)); // Pendiente, sin resultado
        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new ObtenerTableroAnaliticaQueryHandler(dbContext, auditLogger);

        var resultado = await handler.Handle(new ObtenerTableroAnaliticaQuery(), CancellationToken.None);

        Assert.Equal(3, resultado.PorResultado.Count);
        Assert.Contains(resultado.PorResultado, c => c.Resultado == ResultadoCaso.Positivo && c.Cantidad == 2);
        Assert.Contains(resultado.PorResultado, c => c.Resultado == ResultadoCaso.Negativo && c.Cantidad == 1);
        Assert.Contains(resultado.PorResultado, c => c.Resultado == ResultadoCaso.Revision && c.Cantidad == 1);
        Assert.DoesNotContain(resultado.PorResultado, c => c.Cantidad == 5);
    }

    [Fact]
    public async Task ObtenerTableroAnalitica_FiltroPorRangoDeFechas_ExcluyeCasosFueraDelRangoEnLasCuatroDimensiones()
    {
        // "Dado que selecciono un rango de fechas" aplica a los 4 tableros
        // por igual — se prueba una vez con las 4 dimensiones a la vez.
        var dbContext = new TestAppDbContext();
        var dependencia = Guid.NewGuid();
        var tipoIncidente = Guid.NewGuid();
        var analista = Guid.NewGuid();

        var casoDentroDelRango = CrearCaso(new DateOnly(2026, 5, 15), dependencia, tipoIncidente, analista, ResultadoCaso.Positivo);
        var casoAntesDelRango = CrearCaso(new DateOnly(2026, 4, 30), dependencia, tipoIncidente, analista, ResultadoCaso.Positivo);
        var casoDespuesDelRango = CrearCaso(new DateOnly(2026, 6, 1), dependencia, tipoIncidente, analista, ResultadoCaso.Positivo);

        dbContext.CasosAnalisis.AddRange(casoDentroDelRango, casoAntesDelRango, casoDespuesDelRango);
        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new ObtenerTableroAnaliticaQueryHandler(dbContext, auditLogger);

        var resultado = await handler.Handle(
            new ObtenerTableroAnaliticaQuery(
                FechaDesde: new DateOnly(2026, 5, 1),
                FechaHasta: new DateOnly(2026, 5, 31)),
            CancellationToken.None);

        Assert.Equal(1, resultado.PorDependencia.Single().Cantidad);
        Assert.Equal(1, resultado.PorTipoIncidente.Single().Cantidad);
        Assert.Equal(1, resultado.PorAnalista.Single().Cantidad);
        Assert.Equal(1, resultado.PorResultado.Single().Cantidad);
    }

    [Fact]
    public async Task ObtenerTableroAnalitica_SinRangoDeFechas_IncluyeTodosLosCasosSinFiltrar()
    {
        var dbContext = new TestAppDbContext();
        var dependencia = Guid.NewGuid();
        var tipoIncidente = Guid.NewGuid();
        var analista = Guid.NewGuid();

        dbContext.CasosAnalisis.Add(CrearCaso(new DateOnly(2020, 1, 1), dependencia, tipoIncidente, analista));
        dbContext.CasosAnalisis.Add(CrearCaso(new DateOnly(2030, 12, 31), dependencia, tipoIncidente, analista));
        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new ObtenerTableroAnaliticaQueryHandler(dbContext, auditLogger);

        var resultado = await handler.Handle(new ObtenerTableroAnaliticaQuery(), CancellationToken.None);

        Assert.Equal(2, resultado.PorDependencia.Single().Cantidad);
    }

    [Fact]
    public async Task ObtenerTableroAnalitica_CualquierConsulta_RegistraElAccesoEnAuditoria()
    {
        // CLAUDE.md, sección Seguridad: toda query que lea CasoAnalisis debe
        // registrar el evento en AuditLog — no es opcional.
        var dbContext = new TestAppDbContext();
        dbContext.CasosAnalisis.Add(CrearCaso(new DateOnly(2026, 1, 10), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));
        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new ObtenerTableroAnaliticaQueryHandler(dbContext, auditLogger);

        await handler.Handle(new ObtenerTableroAnaliticaQuery(), CancellationToken.None);

        Assert.Single(auditLogger.Registros);
        Assert.Equal("Analitica", auditLogger.Registros[0].Accion);
        Assert.Equal(nameof(CasoAnalisis), auditLogger.Registros[0].Entidad);
        Assert.Null(auditLogger.Registros[0].EntidadId);
    }

    [Fact]
    public void ObtenerTableroAnaliticaQuery_DeclaraAutorizacion_ParaSupervisorYAdmin()
    {
        // HU-06: "Como Supervisor de Equipo Analítica" — el tablero de
        // carga de trabajo del equipo no es un caso de uso de un Analista
        // individual (a diferencia de HU-05, buscar informes).
        var atributo = typeof(ObtenerTableroAnaliticaQuery)
            .GetCustomAttributes(typeof(AutorizarAttribute), inherit: true)
            .Cast<AutorizarAttribute>()
            .SingleOrDefault();

        Assert.NotNull(atributo);
        Assert.Contains(Roles.Supervisor, atributo.Roles);
        Assert.Contains(Roles.Admin, atributo.Roles);
        Assert.DoesNotContain(Roles.Analista, atributo.Roles);
    }
}
