using IGE.Informes.Application.Common.Dtos;
using IGE.Informes.Application.Common.Security;
using IGE.Informes.Application.Personas.Queries.ObtenerHistorialInformesPersona;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Personas;

/// <summary>
/// HU-07 · Ficha 360° de una persona (Épica 02). El escenario Gherkin solo
/// describe explícitamente el caso de Vehículo, pero la HU (título y
/// descripción) exige el mismo criterio para Persona.
/// </summary>
public class ObtenerHistorialInformesPersonaQueryHandlerTests
{
    private static Persona CrearPersonaIdentificada(string dni = "30111222")
    {
        return new Persona(RolPersona.Sospechoso, nombre: "Juan Pérez", dni: dni);
    }

    private static Persona CrearPersonaSinIdentificar()
    {
        return new Persona(RolPersona.Testigo, caracteristicas: "Contextura media, campera roja");
    }

    private static Informe CrearInforme(string idRegistro, DateOnly fechaAnalisis, Guid dependenciaDestinoId)
    {
        return Informe.CrearMigrado(idRegistro, fechaAnalisis, dependenciaDestinoId, Guid.NewGuid());
    }

    [Fact]
    public async Task ObtenerHistorial_PersonaSinEvidencias_DevuelveListaVacia()
    {
        var dbContext = new TestAppDbContext();
        var persona = CrearPersonaIdentificada();
        dbContext.Personas.Add(persona);
        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new ObtenerHistorialInformesPersonaQueryHandler(dbContext, auditLogger);

        var resultado = await handler.Handle(
            new ObtenerHistorialInformesPersonaQuery(persona.Id), CancellationToken.None);

        Assert.Empty(resultado);
    }

    [Fact]
    public async Task ObtenerHistorial_UnInformeConUnaEvidencia_DevuelveEseInforme()
    {
        var dbContext = new TestAppDbContext();
        var dependenciaId = Guid.NewGuid();
        var persona = CrearPersonaIdentificada();
        dbContext.Personas.Add(persona);

        var informe = CrearInforme("1/2026", new DateOnly(2026, 1, 10), dependenciaId);
        dbContext.Informes.Add(informe);

        var evidencia = new Evidencia(1, informe.Id);
        evidencia.VincularPersona(persona.Id);
        dbContext.Evidencias.Add(evidencia);
        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new ObtenerHistorialInformesPersonaQueryHandler(dbContext, auditLogger);

        var resultado = await handler.Handle(
            new ObtenerHistorialInformesPersonaQuery(persona.Id), CancellationToken.None);

        var item = Assert.Single(resultado);
        Assert.Equal(informe.Id, item.InformeId);
        Assert.Equal("1/2026", item.IdRegistro);
        Assert.Equal(new DateOnly(2026, 1, 10), item.FechaAnalisis);
        Assert.Equal(EstadoInforme.Borrador, item.Estado);
        Assert.Equal(dependenciaId, item.DependenciaDestinoId);
        Assert.Equal(1, item.CantidadEvidencias);
    }

    [Fact]
    public async Task ObtenerHistorial_InformeConVariasEvidenciasQueMencionanLaMismaPersona_NoDuplicaElInforme()
    {
        var dbContext = new TestAppDbContext();
        var dependenciaId = Guid.NewGuid();
        var persona = CrearPersonaIdentificada();
        dbContext.Personas.Add(persona);

        var informe = CrearInforme("1/2026", new DateOnly(2026, 1, 10), dependenciaId);
        dbContext.Informes.Add(informe);

        var evidencia1 = new Evidencia(1, informe.Id);
        evidencia1.VincularPersona(persona.Id);
        dbContext.Evidencias.Add(evidencia1);

        var evidencia2 = new Evidencia(2, informe.Id);
        evidencia2.VincularPersona(persona.Id);
        dbContext.Evidencias.Add(evidencia2);
        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new ObtenerHistorialInformesPersonaQueryHandler(dbContext, auditLogger);

        var resultado = await handler.Handle(
            new ObtenerHistorialInformesPersonaQuery(persona.Id), CancellationToken.None);

        var item = Assert.Single(resultado);
        Assert.Equal(informe.Id, item.InformeId);
        Assert.Equal(2, item.CantidadEvidencias);
        Assert.Equal([1, 2], item.NumerosDeImagen.OrderBy(n => n));
    }

    [Fact]
    public async Task ObtenerHistorial_MultiplesInformes_DevuelveOrdenadosPorFechaAnalisisDescendente()
    {
        var dbContext = new TestAppDbContext();
        var dependenciaId = Guid.NewGuid();
        var persona = CrearPersonaIdentificada();
        dbContext.Personas.Add(persona);

        var informeAntiguo = CrearInforme("1/2026", new DateOnly(2026, 1, 10), dependenciaId);
        var informeReciente = CrearInforme("2/2026", new DateOnly(2026, 6, 15), dependenciaId);
        var informeIntermedio = CrearInforme("3/2026", new DateOnly(2026, 3, 20), dependenciaId);
        dbContext.Informes.AddRange(informeAntiguo, informeReciente, informeIntermedio);

        var evidenciaAntigua = new Evidencia(1, informeAntiguo.Id);
        evidenciaAntigua.VincularPersona(persona.Id);
        dbContext.Evidencias.Add(evidenciaAntigua);

        var evidenciaReciente = new Evidencia(1, informeReciente.Id);
        evidenciaReciente.VincularPersona(persona.Id);
        dbContext.Evidencias.Add(evidenciaReciente);

        var evidenciaIntermedia = new Evidencia(1, informeIntermedio.Id);
        evidenciaIntermedia.VincularPersona(persona.Id);
        dbContext.Evidencias.Add(evidenciaIntermedia);
        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new ObtenerHistorialInformesPersonaQueryHandler(dbContext, auditLogger);

        var resultado = (await handler.Handle(
            new ObtenerHistorialInformesPersonaQuery(persona.Id), CancellationToken.None)).ToList();

        Assert.Equal(3, resultado.Count);
        Assert.Equal(informeReciente.Id, resultado[0].InformeId);
        Assert.Equal(informeIntermedio.Id, resultado[1].InformeId);
        Assert.Equal(informeAntiguo.Id, resultado[2].InformeId);
    }

    [Fact]
    public async Task ObtenerHistorial_NoIncluyeInformesDeOtraPersona()
    {
        var dbContext = new TestAppDbContext();
        var dependenciaId = Guid.NewGuid();
        var personaBuscada = CrearPersonaIdentificada("30111222");
        var otraPersona = CrearPersonaIdentificada("28999888");
        dbContext.Personas.Add(personaBuscada);
        dbContext.Personas.Add(otraPersona);

        var informeDeLaBuscada = CrearInforme("1/2026", new DateOnly(2026, 1, 10), dependenciaId);
        var informeDeLaOtra = CrearInforme("2/2026", new DateOnly(2026, 1, 11), dependenciaId);
        dbContext.Informes.Add(informeDeLaBuscada);
        dbContext.Informes.Add(informeDeLaOtra);

        var evidenciaBuscada = new Evidencia(1, informeDeLaBuscada.Id);
        evidenciaBuscada.VincularPersona(personaBuscada.Id);
        dbContext.Evidencias.Add(evidenciaBuscada);

        var evidenciaOtra = new Evidencia(1, informeDeLaOtra.Id);
        evidenciaOtra.VincularPersona(otraPersona.Id);
        dbContext.Evidencias.Add(evidenciaOtra);
        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new ObtenerHistorialInformesPersonaQueryHandler(dbContext, auditLogger);

        var resultado = await handler.Handle(
            new ObtenerHistorialInformesPersonaQuery(personaBuscada.Id), CancellationToken.None);

        Assert.Single(resultado);
        Assert.Equal(informeDeLaBuscada.Id, resultado.Single().InformeId);
    }

    [Fact]
    public async Task ObtenerHistorial_PersonaSinIdentificar_TambienDevuelveSuHistorial()
    {
        // El escenario Gherkin y la HU no distinguen entre Persona
        // identificada (con DNI) y sin identificar (solo características) —
        // la ficha 360° debe funcionar igual para ambas.
        var dbContext = new TestAppDbContext();
        var dependenciaId = Guid.NewGuid();
        var persona = CrearPersonaSinIdentificar();
        dbContext.Personas.Add(persona);

        var informe = CrearInforme("1/2026", new DateOnly(2026, 1, 10), dependenciaId);
        dbContext.Informes.Add(informe);

        var evidencia = new Evidencia(1, informe.Id);
        evidencia.VincularPersona(persona.Id);
        dbContext.Evidencias.Add(evidencia);
        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new ObtenerHistorialInformesPersonaQueryHandler(dbContext, auditLogger);

        var resultado = await handler.Handle(
            new ObtenerHistorialInformesPersonaQuery(persona.Id), CancellationToken.None);

        Assert.Single(resultado);
    }

    [Fact]
    public async Task ObtenerHistorial_CualquierConsulta_RegistraElAccesoEnAuditoriaSobreInforme()
    {
        var dbContext = new TestAppDbContext();
        var dependenciaId = Guid.NewGuid();
        var persona = CrearPersonaIdentificada();
        dbContext.Personas.Add(persona);

        var informe = CrearInforme("1/2026", new DateOnly(2026, 1, 10), dependenciaId);
        dbContext.Informes.Add(informe);

        var evidencia = new Evidencia(1, informe.Id);
        evidencia.VincularPersona(persona.Id);
        dbContext.Evidencias.Add(evidencia);
        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new ObtenerHistorialInformesPersonaQueryHandler(dbContext, auditLogger);

        await handler.Handle(new ObtenerHistorialInformesPersonaQuery(persona.Id), CancellationToken.None);

        Assert.Contains(auditLogger.Registros,
            r => r.Accion == "Lectura" && r.Entidad == nameof(Informe));
    }

    [Fact]
    public async Task ObtenerHistorial_PersonaIdentificadaConDni_RegistraAuditoriaAdicionalSobrePersona()
    {
        // Mismo criterio que BuscarInformesQueryHandler (HU-05, líneas
        // 109-117): consultar el historial de una Persona identificada con
        // DNI es acceso a un dato personal sensible y requiere auditoría
        // explícita adicional sobre la entidad Persona, no solo sobre Informe.
        var dbContext = new TestAppDbContext();
        var dependenciaId = Guid.NewGuid();
        var persona = CrearPersonaIdentificada("30111222");
        dbContext.Personas.Add(persona);

        var informe = CrearInforme("1/2026", new DateOnly(2026, 1, 10), dependenciaId);
        dbContext.Informes.Add(informe);

        var evidencia = new Evidencia(1, informe.Id);
        evidencia.VincularPersona(persona.Id);
        dbContext.Evidencias.Add(evidencia);
        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new ObtenerHistorialInformesPersonaQueryHandler(dbContext, auditLogger);

        await handler.Handle(new ObtenerHistorialInformesPersonaQuery(persona.Id), CancellationToken.None);

        Assert.Contains(auditLogger.Registros,
            r => r.Accion == "Lectura" && r.Entidad == nameof(Persona) && r.EntidadId == persona.Id);
    }

    [Fact]
    public void ObtenerHistorialInformesPersonaQuery_DeclaraAutorizacion_ParaAnalistaSupervisorYAdmin()
    {
        // Mismo criterio que BuscarInformesQuery/ListarCasosQuery — no es
        // exclusivo de Supervisor como HU-06 (tablero de analítica).
        var atributo = typeof(ObtenerHistorialInformesPersonaQuery)
            .GetCustomAttributes(typeof(AutorizarAttribute), inherit: true)
            .Cast<AutorizarAttribute>()
            .SingleOrDefault();

        Assert.NotNull(atributo);
        Assert.Contains(Roles.Analista, atributo.Roles);
        Assert.Contains(Roles.Supervisor, atributo.Roles);
        Assert.Contains(Roles.Admin, atributo.Roles);
    }
}
