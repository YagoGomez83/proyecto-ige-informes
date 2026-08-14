using IGE.Informes.Application.Informes.Queries.BuscarInformes;
using IGE.Informes.Domain.Entities;
using IGE.Informes.Infrastructure.Busqueda;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Informes;

/// <summary>
/// HU-05 · Buscar informes por múltiples criterios (Épica 02). Cubre acá
/// SOLO los filtros que funcionan igual en EF Core InMemory que en Postgres
/// real: DependenciaId, TipoIncidenteId, DominioVehiculo, DniOPersona y
/// rango de fechas, más sus combinaciones y el registro de auditoría.
///
/// El escenario Gherkin "Búsqueda por texto libre en el relato" usa
/// EF.Functions.ToTsVector/PlainToTsQuery (ADR-002) — una feature específica
/// del motor de Postgres que EF Core InMemory no traduce/soporta. Ese
/// escenario se cubre exclusivamente en
/// tests/IGE.Informes.IntegrationTests/Informes/BuscarInformesTextoLibreTests.cs
/// contra un Postgres real vía Testcontainers (mismo criterio ya documentado
/// en memoria del proyecto: "EF Core InMemory no es fiel a Postgres real
/// para features específicas del motor").
/// </summary>
public class BuscarInformesQueryHandlerTests
{
    private static Informe CrearInforme(
        string idRegistro,
        DateOnly fechaAnalisis,
        Guid dependenciaDestinoId,
        Guid? casoAnalisisId = null)
    {
        return casoAnalisisId is null
            ? Informe.CrearMigrado(idRegistro, fechaAnalisis, dependenciaDestinoId, Guid.NewGuid())
            : new Informe(idRegistro, fechaAnalisis, casoAnalisisId.Value, dependenciaDestinoId, Guid.NewGuid());
    }

    [Fact]
    public async Task BuscarInformes_SinFiltros_DevuelveTodosLosInformes()
    {
        var dbContext = new TestAppDbContext();
        var dependenciaId = Guid.NewGuid();

        dbContext.Informes.Add(CrearInforme("1/2026", new DateOnly(2026, 1, 10), dependenciaId));
        dbContext.Informes.Add(CrearInforme("2/2026", new DateOnly(2026, 2, 10), dependenciaId));
        dbContext.Informes.Add(CrearInforme("3/2026", new DateOnly(2026, 3, 10), dependenciaId));
        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new BuscarInformesQueryHandler(dbContext, auditLogger);

        var resultado = await handler.Handle(new BuscarInformesQuery(), CancellationToken.None);

        Assert.Equal(3, resultado.Count);
    }

    [Fact]
    public async Task BuscarInformes_FiltroPorDependenciaId_DevuelveSoloLosInformesDeEsaDependencia()
    {
        var dbContext = new TestAppDbContext();
        var dependenciaBuscada = Guid.NewGuid();
        var otraDependencia = Guid.NewGuid();

        var informeEnDependencia = CrearInforme("1/2026", new DateOnly(2026, 1, 10), dependenciaBuscada);
        dbContext.Informes.Add(informeEnDependencia);
        dbContext.Informes.Add(CrearInforme("2/2026", new DateOnly(2026, 1, 11), otraDependencia));
        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new BuscarInformesQueryHandler(dbContext, auditLogger);

        var resultado = await handler.Handle(
            new BuscarInformesQuery(DependenciaId: dependenciaBuscada),
            CancellationToken.None);

        Assert.Single(resultado);
        Assert.Equal(informeEnDependencia.Id, resultado.Single().Id);
    }

    [Fact]
    public async Task BuscarInformes_FiltroPorTipoIncidenteId_DevuelveSoloInformesCuyoCasoTieneEseTipoYExcluyeMigrados()
    {
        var dbContext = new TestAppDbContext();
        var dependenciaId = Guid.NewGuid();
        var tipoIncidenteBuscado = Guid.NewGuid();
        var otroTipoIncidente = Guid.NewGuid();

        var casoConTipoBuscado = new CasoAnalisis(new DateOnly(2026, 1, 5), dependenciaId, tipoIncidenteBuscado, Guid.NewGuid());
        var casoConOtroTipo = new CasoAnalisis(new DateOnly(2026, 1, 6), dependenciaId, otroTipoIncidente, Guid.NewGuid());
        dbContext.CasosAnalisis.Add(casoConTipoBuscado);
        dbContext.CasosAnalisis.Add(casoConOtroTipo);

        var informeConTipoBuscado = CrearInforme("1/2026", new DateOnly(2026, 1, 10), dependenciaId, casoConTipoBuscado.Id);
        dbContext.Informes.Add(informeConTipoBuscado);
        dbContext.Informes.Add(CrearInforme("2/2026", new DateOnly(2026, 1, 11), dependenciaId, casoConOtroTipo.Id));
        dbContext.Informes.Add(CrearInforme("3/2026-migrado", new DateOnly(2026, 1, 12), dependenciaId));
        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new BuscarInformesQueryHandler(dbContext, auditLogger);

        var resultado = await handler.Handle(
            new BuscarInformesQuery(TipoIncidenteId: tipoIncidenteBuscado),
            CancellationToken.None);

        Assert.Single(resultado);
        Assert.Equal(informeConTipoBuscado.Id, resultado.Single().Id);
    }

    [Fact]
    public async Task BuscarInformes_FiltroPorDominioVehiculo_DevuelveInformeConEvidenciaQueVinculaEseVehiculo()
    {
        var dbContext = new TestAppDbContext();
        var dependenciaId = Guid.NewGuid();

        var vehiculoBuscado = new Vehiculo(
            "Ford", "Fiesta", "Gris", CertezaDominio.Confirmado, AccionARealizar.Identificar, "Comisaría 2°", TipoVehiculo.Auto, dominio: "IAK796");
        var otroVehiculo = new Vehiculo(
            "Renault", "Clio", "Azul", CertezaDominio.Confirmado, AccionARealizar.Identificar, "Comisaría 2°", TipoVehiculo.Auto, dominio: "ABC123");
        dbContext.Vehiculos.Add(vehiculoBuscado);
        dbContext.Vehiculos.Add(otroVehiculo);

        var informeConVehiculo = CrearInforme("1/2026", new DateOnly(2026, 1, 10), dependenciaId);
        var informeSinVehiculo = CrearInforme("2/2026", new DateOnly(2026, 1, 11), dependenciaId);
        dbContext.Informes.Add(informeConVehiculo);
        dbContext.Informes.Add(informeSinVehiculo);

        var evidenciaConVehiculoBuscado = new Evidencia(1, informeConVehiculo.Id);
        evidenciaConVehiculoBuscado.VincularVehiculo(vehiculoBuscado.Id);
        dbContext.Evidencias.Add(evidenciaConVehiculoBuscado);

        var evidenciaConOtroVehiculo = new Evidencia(1, informeSinVehiculo.Id);
        evidenciaConOtroVehiculo.VincularVehiculo(otroVehiculo.Id);
        dbContext.Evidencias.Add(evidenciaConOtroVehiculo);

        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new BuscarInformesQueryHandler(dbContext, auditLogger);

        var resultado = await handler.Handle(
            new BuscarInformesQuery(DominioVehiculo: "IAK796"),
            CancellationToken.None);

        Assert.Single(resultado);
        Assert.Equal(informeConVehiculo.Id, resultado.Single().Id);
    }

    [Fact]
    public async Task BuscarInformes_FiltroPorDominioVehiculo_NormalizaEspaciosYMayusculas()
    {
        var dbContext = new TestAppDbContext();
        var dependenciaId = Guid.NewGuid();

        var vehiculo = new Vehiculo(
            "Ford", "Fiesta", "Gris", CertezaDominio.Confirmado, AccionARealizar.Identificar, "Comisaría 2°", TipoVehiculo.Auto, dominio: "IAK 796");
        dbContext.Vehiculos.Add(vehiculo);

        var informe = CrearInforme("1/2026", new DateOnly(2026, 1, 10), dependenciaId);
        dbContext.Informes.Add(informe);

        var evidencia = new Evidencia(1, informe.Id);
        evidencia.VincularVehiculo(vehiculo.Id);
        dbContext.Evidencias.Add(evidencia);

        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new BuscarInformesQueryHandler(dbContext, auditLogger);

        var resultado = await handler.Handle(
            new BuscarInformesQuery(DominioVehiculo: "iak796"),
            CancellationToken.None);

        Assert.Single(resultado);
        Assert.Equal(informe.Id, resultado.Single().Id);
    }

    [Fact]
    public async Task BuscarInformes_FiltroPorDniExacto_DevuelveInformeConEvidenciaQueVinculaEsaPersona()
    {
        var dbContext = new TestAppDbContext();
        var dependenciaId = Guid.NewGuid();

        var personaBuscada = new Persona(RolPersona.Sospechoso, nombre: "Juan Pérez", dni: "30111222");
        var otraPersona = new Persona(RolPersona.Testigo, nombre: "María Gómez", dni: "28999888");
        dbContext.Personas.Add(personaBuscada);
        dbContext.Personas.Add(otraPersona);

        var informeConPersona = CrearInforme("1/2026", new DateOnly(2026, 1, 10), dependenciaId);
        var informeSinPersona = CrearInforme("2/2026", new DateOnly(2026, 1, 11), dependenciaId);
        dbContext.Informes.Add(informeConPersona);
        dbContext.Informes.Add(informeSinPersona);

        var evidenciaConPersonaBuscada = new Evidencia(1, informeConPersona.Id);
        evidenciaConPersonaBuscada.VincularPersona(personaBuscada.Id);
        dbContext.Evidencias.Add(evidenciaConPersonaBuscada);

        var evidenciaConOtraPersona = new Evidencia(1, informeSinPersona.Id);
        evidenciaConOtraPersona.VincularPersona(otraPersona.Id);
        dbContext.Evidencias.Add(evidenciaConOtraPersona);

        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new BuscarInformesQueryHandler(dbContext, auditLogger);

        var resultado = await handler.Handle(
            new BuscarInformesQuery(DniOPersona: "30111222"),
            CancellationToken.None);

        Assert.Single(resultado);
        Assert.Equal(informeConPersona.Id, resultado.Single().Id);
    }

    [Fact]
    public async Task BuscarInformes_FiltroPorNombreParcial_DevuelveInformeConEvidenciaQueVinculaEsaPersona()
    {
        var dbContext = new TestAppDbContext();
        var dependenciaId = Guid.NewGuid();

        var personaBuscada = new Persona(RolPersona.Sospechoso, nombre: "Juan Pérez", dni: "30111222");
        var otraPersona = new Persona(RolPersona.Testigo, nombre: "María Gómez", dni: "28999888");
        dbContext.Personas.Add(personaBuscada);
        dbContext.Personas.Add(otraPersona);

        var informeConPersona = CrearInforme("1/2026", new DateOnly(2026, 1, 10), dependenciaId);
        var informeSinPersona = CrearInforme("2/2026", new DateOnly(2026, 1, 11), dependenciaId);
        dbContext.Informes.Add(informeConPersona);
        dbContext.Informes.Add(informeSinPersona);

        var evidenciaConPersonaBuscada = new Evidencia(1, informeConPersona.Id);
        evidenciaConPersonaBuscada.VincularPersona(personaBuscada.Id);
        dbContext.Evidencias.Add(evidenciaConPersonaBuscada);

        var evidenciaConOtraPersona = new Evidencia(1, informeSinPersona.Id);
        evidenciaConOtraPersona.VincularPersona(otraPersona.Id);
        dbContext.Evidencias.Add(evidenciaConOtraPersona);

        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new BuscarInformesQueryHandler(dbContext, auditLogger);

        var resultado = await handler.Handle(
            new BuscarInformesQuery(DniOPersona: "Pérez"),
            CancellationToken.None);

        Assert.Single(resultado);
        Assert.Equal(informeConPersona.Id, resultado.Single().Id);
    }

    [Fact]
    public async Task BuscarInformes_FiltroPorRangoDeFechas_DevuelveSoloInformesDentroDelRangoInclusive()
    {
        var dbContext = new TestAppDbContext();
        var dependenciaId = Guid.NewGuid();

        var informeAntesDelRango = CrearInforme("1/2026", new DateOnly(2026, 3, 31), dependenciaId);
        var informeInicioRango = CrearInforme("2/2026", new DateOnly(2026, 4, 1), dependenciaId);
        var informeDentroDelRango = CrearInforme("3/2026", new DateOnly(2026, 5, 15), dependenciaId);
        var informeFinRango = CrearInforme("4/2026", new DateOnly(2026, 6, 30), dependenciaId);
        var informeDespuesDelRango = CrearInforme("5/2026", new DateOnly(2026, 7, 1), dependenciaId);

        dbContext.Informes.AddRange(
            informeAntesDelRango, informeInicioRango, informeDentroDelRango, informeFinRango, informeDespuesDelRango);
        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new BuscarInformesQueryHandler(dbContext, auditLogger);

        var resultado = await handler.Handle(
            new BuscarInformesQuery(
                FechaDesde: new DateOnly(2026, 4, 1),
                FechaHasta: new DateOnly(2026, 6, 30)),
            CancellationToken.None);

        Assert.Equal(3, resultado.Count);
        Assert.Contains(resultado, r => r.Id == informeInicioRango.Id);
        Assert.Contains(resultado, r => r.Id == informeDentroDelRango.Id);
        Assert.Contains(resultado, r => r.Id == informeFinRango.Id);
        Assert.DoesNotContain(resultado, r => r.Id == informeAntesDelRango.Id);
        Assert.DoesNotContain(resultado, r => r.Id == informeDespuesDelRango.Id);
    }

    [Fact]
    public async Task BuscarInformes_CombinacionDependenciaYRangoDeFechas_AplicaAmbosFiltrosConAnd()
    {
        // Escenario Gherkin "Combinación de filtros": Dependencia = "Comisaría
        // Seccional Primera" + rango de fechas = último trimestre. Acá se
        // valida el AND entre ambos criterios con IDs, no con el nombre
        // literal de la Dependencia (el nombre se resuelve en otra capa).
        var dbContext = new TestAppDbContext();
        var dependenciaBuscada = Guid.NewGuid();
        var otraDependencia = Guid.NewGuid();

        var informeCumpleAmbos = CrearInforme("1/2026", new DateOnly(2026, 5, 10), dependenciaBuscada);
        var informeDependenciaCorrectaFechaFueraDeRango = CrearInforme("2/2026", new DateOnly(2026, 1, 10), dependenciaBuscada);
        var informeFechaCorrectaDependenciaIncorrecta = CrearInforme("3/2026", new DateOnly(2026, 5, 12), otraDependencia);

        dbContext.Informes.AddRange(
            informeCumpleAmbos, informeDependenciaCorrectaFechaFueraDeRango, informeFechaCorrectaDependenciaIncorrecta);
        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new BuscarInformesQueryHandler(dbContext, auditLogger);

        var resultado = await handler.Handle(
            new BuscarInformesQuery(
                DependenciaId: dependenciaBuscada,
                FechaDesde: new DateOnly(2026, 4, 1),
                FechaHasta: new DateOnly(2026, 6, 30)),
            CancellationToken.None);

        Assert.Single(resultado);
        Assert.Equal(informeCumpleAmbos.Id, resultado.Single().Id);
    }

    [Fact]
    public async Task BuscarInformes_CualquierBusqueda_RegistraElAccesoEnAuditoria()
    {
        var dbContext = new TestAppDbContext();
        var dependenciaId = Guid.NewGuid();
        dbContext.Informes.Add(CrearInforme("1/2026", new DateOnly(2026, 1, 10), dependenciaId));
        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new BuscarInformesQueryHandler(dbContext, auditLogger);

        await handler.Handle(new BuscarInformesQuery(DependenciaId: dependenciaId), CancellationToken.None);

        Assert.Single(auditLogger.Registros);
        Assert.Equal("Busqueda", auditLogger.Registros[0].Accion);
        Assert.Equal(nameof(Informe), auditLogger.Registros[0].Entidad);
        Assert.Null(auditLogger.Registros[0].EntidadId);
    }

    [Fact]
    public async Task BuscarInformes_FiltroPorDniOPersona_RegistraAuditoriaAdicionalSobrePersona()
    {
        var dbContext = new TestAppDbContext();
        var dependenciaId = Guid.NewGuid();

        var persona = new Persona(RolPersona.Sospechoso, nombre: "Juan Pérez", dni: "30111222");
        dbContext.Personas.Add(persona);

        var informe = CrearInforme("1/2026", new DateOnly(2026, 1, 10), dependenciaId);
        dbContext.Informes.Add(informe);

        var evidencia = new Evidencia(1, informe.Id);
        evidencia.VincularPersona(persona.Id);
        dbContext.Evidencias.Add(evidencia);

        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new BuscarInformesQueryHandler(dbContext, auditLogger);

        await handler.Handle(new BuscarInformesQuery(DniOPersona: "30111222"), CancellationToken.None);

        Assert.Equal(2, auditLogger.Registros.Count);
        Assert.Contains(auditLogger.Registros, r => r.Accion == "Busqueda" && r.Entidad == nameof(Persona));
        Assert.Contains(auditLogger.Registros, r => r.Accion == "Busqueda" && r.Entidad == nameof(Informe));
    }

    [Fact]
    public async Task BuscarInformes_RelatoLargo_LoTruncaEnElListado()
    {
        var dbContext = new TestAppDbContext();
        var dependenciaId = Guid.NewGuid();

        var relatoLargo = new string('A', 250);
        var informe = CrearInforme("1/2026", new DateOnly(2026, 1, 10), dependenciaId);
        informe.CompletarRelato(relatoLargo);
        dbContext.Informes.Add(informe);
        await dbContext.SaveChangesAsync();

        var handler = new BuscarInformesQueryHandler(dbContext, new FakeAuditLogger());

        var resultado = await handler.Handle(new BuscarInformesQuery(DependenciaId: dependenciaId), CancellationToken.None);

        var relatoDevuelto = resultado.Single().Relato;
        Assert.NotNull(relatoDevuelto);
        Assert.True(relatoDevuelto.Length < relatoLargo.Length);
        Assert.EndsWith("…", relatoDevuelto);
    }
}
