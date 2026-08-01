using IGE.Informes.Application.Informes.Queries.ObtenerInformePorId;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Informes;

public class ObtenerInformePorIdQueryHandlerTests
{
    [Fact]
    public async Task Devuelve_el_informe_y_registra_la_lectura_en_AuditLog()
    {
        var dbContext = new TestAppDbContext();
        var informe = new Informe("290/2026", new DateOnly(2026, 7, 21), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        dbContext.Informes.Add(informe);
        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new ObtenerInformePorIdQueryHandler(dbContext, auditLogger, new FakeFileStorage());

        var dto = await handler.Handle(new ObtenerInformePorIdQuery(informe.Id), CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal("290/2026", dto.IdRegistro);
        Assert.Single(auditLogger.Registros);
        Assert.Equal(("Lectura", nameof(Informe), informe.Id), auditLogger.Registros[0]);
    }

    [Fact]
    public async Task Genera_una_url_de_vista_previa_del_pdf_cuando_el_informe_ya_lo_tiene_cargado()
    {
        var dbContext = new TestAppDbContext();
        var informe = new Informe("290/2026", new DateOnly(2026, 7, 21), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        informe.AsignarPdf("informes/290-2026.pdf");
        dbContext.Informes.Add(informe);
        await dbContext.SaveChangesAsync();

        var handler = new ObtenerInformePorIdQueryHandler(dbContext, new FakeAuditLogger(), new FakeFileStorage());

        var dto = await handler.Handle(new ObtenerInformePorIdQuery(informe.Id), CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal("https://fake-storage.local/informes/290-2026.pdf", dto.PdfUrl);
    }

    [Fact]
    public async Task No_genera_url_de_vista_previa_cuando_el_informe_todavia_no_tiene_pdf()
    {
        var dbContext = new TestAppDbContext();
        var informe = new Informe("290/2026", new DateOnly(2026, 7, 21), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        dbContext.Informes.Add(informe);
        await dbContext.SaveChangesAsync();

        var handler = new ObtenerInformePorIdQueryHandler(dbContext, new FakeAuditLogger(), new FakeFileStorage());

        var dto = await handler.Handle(new ObtenerInformePorIdQuery(informe.Id), CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Null(dto.PdfUrl);
    }

    [Fact]
    public async Task Informe_inexistente_devuelve_null_pero_igual_registra_el_intento_de_lectura()
    {
        var dbContext = new TestAppDbContext();
        var auditLogger = new FakeAuditLogger();
        var handler = new ObtenerInformePorIdQueryHandler(dbContext, auditLogger, new FakeFileStorage());

        var idInexistente = Guid.NewGuid();
        var dto = await handler.Handle(new ObtenerInformePorIdQuery(idInexistente), CancellationToken.None);

        Assert.Null(dto);
        Assert.Single(auditLogger.Registros);
    }

    [Fact]
    public async Task Sin_evidencias_vinculadas_devuelve_colecciones_vacias_de_vehiculos_y_personas()
    {
        var dbContext = new TestAppDbContext();
        var informe = new Informe("290/2026", new DateOnly(2026, 7, 21), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        dbContext.Informes.Add(informe);
        await dbContext.SaveChangesAsync();

        var handler = new ObtenerInformePorIdQueryHandler(dbContext, new FakeAuditLogger(), new FakeFileStorage());

        var dto = await handler.Handle(new ObtenerInformePorIdQuery(informe.Id), CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Empty(dto.VehiculosVinculados);
        Assert.Empty(dto.PersonasVinculadas);
    }

    [Fact]
    public async Task Devuelve_los_vehiculos_y_personas_vinculados_via_Evidencia_sin_duplicar()
    {
        var dbContext = new TestAppDbContext();
        var informe = new Informe("290/2026", new DateOnly(2026, 7, 21), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        dbContext.Informes.Add(informe);

        var vehiculo = new Vehiculo("Ford", "Fiesta", "Gris", CertezaDominio.Confirmado, AccionARealizar.Identificar, "Comisaría 2°", dominio: "ABC123");
        var persona = new Persona(RolPersona.Testigo, nombre: "Juan Pérez");
        dbContext.Vehiculos.Add(vehiculo);
        dbContext.Personas.Add(persona);

        var evidencia1 = new Evidencia(1, informe.Id);
        evidencia1.VincularVehiculo(vehiculo.Id);
        evidencia1.VincularPersona(persona.Id);
        dbContext.Evidencias.Add(evidencia1);

        // Segunda Evidencia con el mismo Vehículo/Persona — no debe duplicar
        // en el resultado.
        var evidencia2 = new Evidencia(2, informe.Id);
        evidencia2.VincularVehiculo(vehiculo.Id);
        evidencia2.VincularPersona(persona.Id);
        dbContext.Evidencias.Add(evidencia2);

        await dbContext.SaveChangesAsync();

        var handler = new ObtenerInformePorIdQueryHandler(dbContext, new FakeAuditLogger(), new FakeFileStorage());

        var dto = await handler.Handle(new ObtenerInformePorIdQuery(informe.Id), CancellationToken.None);

        Assert.NotNull(dto);
        var vehiculoVinculado = Assert.Single(dto.VehiculosVinculados);
        Assert.Equal(vehiculo.Id, vehiculoVinculado.Id);
        Assert.Equal("ABC123", vehiculoVinculado.Dominio);

        var personaVinculada = Assert.Single(dto.PersonasVinculadas);
        Assert.Equal(persona.Id, personaVinculada.Id);
        Assert.Equal("Juan Pérez", personaVinculada.Nombre);
    }

    [Fact]
    public async Task Devuelve_los_datos_de_la_Causa_cuando_el_informe_tiene_una_asignada()
    {
        var dbContext = new TestAppDbContext();
        var causa = new Causa("AV. INFRACCION LEY 23.737", "7070029/26", "Primera Circunscripción");
        dbContext.Causas.Add(causa);
        await dbContext.SaveChangesAsync();

        var informe = new Informe("290/2026", new DateOnly(2026, 7, 21), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), causa.Id);
        dbContext.Informes.Add(informe);
        await dbContext.SaveChangesAsync();

        var handler = new ObtenerInformePorIdQueryHandler(dbContext, new FakeAuditLogger(), new FakeFileStorage());

        var dto = await handler.Handle(new ObtenerInformePorIdQuery(informe.Id), CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal("AV. INFRACCION LEY 23.737", dto.CausaCaratula);
        Assert.Equal("7070029/26", dto.CausaNroPiezaSumarial);
        Assert.Equal("Primera Circunscripción", dto.CausaCircunscripcionJudicial);
    }
}
