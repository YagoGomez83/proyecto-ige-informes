using IGE.Informes.Application.Common.Dtos;
using IGE.Informes.Application.Common.Security;
using IGE.Informes.Application.Vehiculos.Queries.ObtenerHistorialInformesVehiculo;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Vehiculos;

/// <summary>
/// HU-07 · Ficha 360° de un vehículo (Épica 02). Escenario Gherkin "Ver
/// historial de un vehículo": la ficha debe listar todos los
/// informes/evidencias donde aparece el vehículo, ordenados cronológicamente.
/// Cubre acá solo lo que EF Core InMemory traduce igual que Postgres real —
/// el filtro es un Contains con un Guid fijo (no una lista externa), que se
/// confirma explícitamente contra Postgres real en
/// tests/IGE.Informes.IntegrationTests/Vehiculos/ObtenerHistorialInformesVehiculoQueryHandlerTests.cs.
/// </summary>
public class ObtenerHistorialInformesVehiculoQueryHandlerTests
{
    private static Vehiculo CrearVehiculo(string dominio = "MRK064")
    {
        return new Vehiculo(
            "Ford", "Fiesta", "Gris", CertezaDominio.Confirmado, AccionARealizar.Identificar, "Comisaría 2°", dominio: dominio);
    }

    private static Informe CrearInforme(string idRegistro, DateOnly fechaAnalisis, Guid dependenciaDestinoId)
    {
        return Informe.CrearMigrado(idRegistro, fechaAnalisis, dependenciaDestinoId, Guid.NewGuid());
    }

    [Fact]
    public async Task ObtenerHistorial_VehiculoSinEvidencias_DevuelveListaVacia()
    {
        var dbContext = new TestAppDbContext();
        var vehiculo = CrearVehiculo();
        dbContext.Vehiculos.Add(vehiculo);
        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new ObtenerHistorialInformesVehiculoQueryHandler(dbContext, auditLogger);

        var resultado = await handler.Handle(
            new ObtenerHistorialInformesVehiculoQuery(vehiculo.Id), CancellationToken.None);

        Assert.Empty(resultado);
    }

    [Fact]
    public async Task ObtenerHistorial_UnInformeConUnaEvidencia_DevuelveEseInforme()
    {
        var dbContext = new TestAppDbContext();
        var dependenciaId = Guid.NewGuid();
        var vehiculo = CrearVehiculo();
        dbContext.Vehiculos.Add(vehiculo);

        var informe = CrearInforme("1/2026", new DateOnly(2026, 1, 10), dependenciaId);
        dbContext.Informes.Add(informe);

        var evidencia = new Evidencia(1, informe.Id);
        evidencia.VincularVehiculo(vehiculo.Id);
        dbContext.Evidencias.Add(evidencia);
        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new ObtenerHistorialInformesVehiculoQueryHandler(dbContext, auditLogger);

        var resultado = await handler.Handle(
            new ObtenerHistorialInformesVehiculoQuery(vehiculo.Id), CancellationToken.None);

        var item = Assert.Single(resultado);
        Assert.Equal(informe.Id, item.InformeId);
        Assert.Equal("1/2026", item.IdRegistro);
        Assert.Equal(new DateOnly(2026, 1, 10), item.FechaAnalisis);
        Assert.Equal(EstadoInforme.Borrador, item.Estado);
        Assert.Equal(dependenciaId, item.DependenciaDestinoId);
        Assert.Equal(1, item.CantidadEvidencias);
    }

    [Fact]
    public async Task ObtenerHistorial_InformeConVariasEvidenciasQueMencionanElMismoVehiculo_NoDuplicaElInforme()
    {
        var dbContext = new TestAppDbContext();
        var dependenciaId = Guid.NewGuid();
        var vehiculo = CrearVehiculo();
        dbContext.Vehiculos.Add(vehiculo);

        var informe = CrearInforme("1/2026", new DateOnly(2026, 1, 10), dependenciaId);
        dbContext.Informes.Add(informe);

        var evidencia1 = new Evidencia(1, informe.Id);
        evidencia1.VincularVehiculo(vehiculo.Id);
        dbContext.Evidencias.Add(evidencia1);

        var evidencia2 = new Evidencia(2, informe.Id);
        evidencia2.VincularVehiculo(vehiculo.Id);
        dbContext.Evidencias.Add(evidencia2);
        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new ObtenerHistorialInformesVehiculoQueryHandler(dbContext, auditLogger);

        var resultado = await handler.Handle(
            new ObtenerHistorialInformesVehiculoQuery(vehiculo.Id), CancellationToken.None);

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
        var vehiculo = CrearVehiculo();
        dbContext.Vehiculos.Add(vehiculo);

        var informeAntiguo = CrearInforme("1/2026", new DateOnly(2026, 1, 10), dependenciaId);
        var informeReciente = CrearInforme("2/2026", new DateOnly(2026, 6, 15), dependenciaId);
        var informeIntermedio = CrearInforme("3/2026", new DateOnly(2026, 3, 20), dependenciaId);
        dbContext.Informes.AddRange(informeAntiguo, informeReciente, informeIntermedio);

        var evidenciaAntigua = new Evidencia(1, informeAntiguo.Id);
        evidenciaAntigua.VincularVehiculo(vehiculo.Id);
        dbContext.Evidencias.Add(evidenciaAntigua);

        var evidenciaReciente = new Evidencia(1, informeReciente.Id);
        evidenciaReciente.VincularVehiculo(vehiculo.Id);
        dbContext.Evidencias.Add(evidenciaReciente);

        var evidenciaIntermedia = new Evidencia(1, informeIntermedio.Id);
        evidenciaIntermedia.VincularVehiculo(vehiculo.Id);
        dbContext.Evidencias.Add(evidenciaIntermedia);
        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new ObtenerHistorialInformesVehiculoQueryHandler(dbContext, auditLogger);

        var resultado = (await handler.Handle(
            new ObtenerHistorialInformesVehiculoQuery(vehiculo.Id), CancellationToken.None)).ToList();

        Assert.Equal(3, resultado.Count);
        Assert.Equal(informeReciente.Id, resultado[0].InformeId);
        Assert.Equal(informeIntermedio.Id, resultado[1].InformeId);
        Assert.Equal(informeAntiguo.Id, resultado[2].InformeId);
    }

    [Fact]
    public async Task ObtenerHistorial_NoIncluyeInformesDeOtroVehiculo()
    {
        var dbContext = new TestAppDbContext();
        var dependenciaId = Guid.NewGuid();
        var vehiculoBuscado = CrearVehiculo("MRK064");
        var otroVehiculo = CrearVehiculo("ABC123");
        dbContext.Vehiculos.Add(vehiculoBuscado);
        dbContext.Vehiculos.Add(otroVehiculo);

        var informeDelBuscado = CrearInforme("1/2026", new DateOnly(2026, 1, 10), dependenciaId);
        var informeDelOtro = CrearInforme("2/2026", new DateOnly(2026, 1, 11), dependenciaId);
        dbContext.Informes.Add(informeDelBuscado);
        dbContext.Informes.Add(informeDelOtro);

        var evidenciaBuscada = new Evidencia(1, informeDelBuscado.Id);
        evidenciaBuscada.VincularVehiculo(vehiculoBuscado.Id);
        dbContext.Evidencias.Add(evidenciaBuscada);

        var evidenciaOtro = new Evidencia(1, informeDelOtro.Id);
        evidenciaOtro.VincularVehiculo(otroVehiculo.Id);
        dbContext.Evidencias.Add(evidenciaOtro);
        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new ObtenerHistorialInformesVehiculoQueryHandler(dbContext, auditLogger);

        var resultado = await handler.Handle(
            new ObtenerHistorialInformesVehiculoQuery(vehiculoBuscado.Id), CancellationToken.None);

        Assert.Single(resultado);
        Assert.Equal(informeDelBuscado.Id, resultado.Single().InformeId);
    }

    [Fact]
    public async Task ObtenerHistorial_CualquierConsulta_RegistraElAccesoEnAuditoriaSobreInforme()
    {
        var dbContext = new TestAppDbContext();
        var dependenciaId = Guid.NewGuid();
        var vehiculo = CrearVehiculo();
        dbContext.Vehiculos.Add(vehiculo);

        var informe = CrearInforme("1/2026", new DateOnly(2026, 1, 10), dependenciaId);
        dbContext.Informes.Add(informe);

        var evidencia = new Evidencia(1, informe.Id);
        evidencia.VincularVehiculo(vehiculo.Id);
        dbContext.Evidencias.Add(evidencia);
        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new ObtenerHistorialInformesVehiculoQueryHandler(dbContext, auditLogger);

        await handler.Handle(new ObtenerHistorialInformesVehiculoQuery(vehiculo.Id), CancellationToken.None);

        Assert.Contains(auditLogger.Registros,
            r => r.Accion == "Lectura" && r.Entidad == nameof(Informe));
    }

    [Fact]
    public async Task ObtenerHistorial_CualquierConsulta_RegistraElAccesoEnAuditoriaSobreVehiculo()
    {
        // CLAUDE.md, sección Seguridad: toda query que lea Vehiculo debe
        // registrar el evento en AuditLog, sin excepción — hallazgo del
        // security-reviewer sobre la primera versión de este Handler.
        var dbContext = new TestAppDbContext();
        var vehiculo = CrearVehiculo();
        dbContext.Vehiculos.Add(vehiculo);
        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new ObtenerHistorialInformesVehiculoQueryHandler(dbContext, auditLogger);

        await handler.Handle(new ObtenerHistorialInformesVehiculoQuery(vehiculo.Id), CancellationToken.None);

        Assert.Contains(auditLogger.Registros,
            r => r.Accion == "Lectura" && r.Entidad == nameof(Vehiculo) && r.EntidadId == vehiculo.Id);
    }

    [Fact]
    public void ObtenerHistorialInformesVehiculoQuery_DeclaraAutorizacion_ParaAnalistaSupervisorYAdmin()
    {
        // Mismo criterio que BuscarInformesQuery/ListarCasosQuery — no es
        // exclusivo de Supervisor como HU-06 (tablero de analítica).
        var atributo = typeof(ObtenerHistorialInformesVehiculoQuery)
            .GetCustomAttributes(typeof(AutorizarAttribute), inherit: true)
            .Cast<AutorizarAttribute>()
            .SingleOrDefault();

        Assert.NotNull(atributo);
        Assert.Contains(Roles.Analista, atributo.Roles);
        Assert.Contains(Roles.Supervisor, atributo.Roles);
        Assert.Contains(Roles.Admin, atributo.Roles);
    }
}
