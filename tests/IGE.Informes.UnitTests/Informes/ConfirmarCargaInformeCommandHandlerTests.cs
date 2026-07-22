using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Informes.Commands.ConfirmarCargaInforme;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Informes;

public class ConfirmarCargaInformeCommandHandlerTests
{
    private static readonly Guid UsuarioId = Guid.NewGuid();
    private static readonly byte[] ContenidoPdfFalso = "%PDF-1.4 contenido de prueba"u8.ToArray();

    private static async Task<(TestAppDbContext DbContext, CasoAnalisis Caso, Dependencia Dependencia)> PrepararAsync()
    {
        var dbContext = new TestAppDbContext();
        var caso = new CasoAnalisis(new DateOnly(2026, 7, 21), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var dependencia = new Dependencia("Comisaría 2°", TipoDependencia.Comisaria);

        dbContext.CasosAnalisis.Add(caso);
        dbContext.Dependencias.Add(dependencia);
        await dbContext.SaveChangesAsync();

        return (dbContext, caso, dependencia);
    }

    private static ConfirmarCargaInformeCommand CrearCommand(Guid casoId, Guid dependenciaId, bool reemplazar = false) => new(
        ContenidoPdfFalso,
        "290-2026.pdf",
        "290/2026",
        new DateOnly(2026, 7, 14),
        casoId,
        dependenciaId,
        "AV. INFRACCION LEY 23.737",
        "7070029/26",
        "Primera Circunscripción",
        "Se procede a realizar el análisis...",
        reemplazar,
        [new(1, null, null, "Captura del comercio")]);

    [Fact]
    public async Task Crea_el_Informe_en_Borrador_con_Causa_relato_pdf_y_evidencias()
    {
        var (dbContext, caso, dependencia) = await PrepararAsync();
        var fileStorage = new FakeFileStorage();
        var handler = new ConfirmarCargaInformeCommandHandler(dbContext, new FakeCurrentUserService(UsuarioId), fileStorage);

        var informeId = await handler.Handle(CrearCommand(caso.Id, dependencia.Id), CancellationToken.None);

        var informe = await dbContext.Informes.FindAsync(informeId);
        Assert.NotNull(informe);
        Assert.Equal(EstadoInforme.Borrador, informe.Estado);
        Assert.Equal("290/2026", informe.IdRegistro);
        Assert.NotNull(informe.CausaId);
        Assert.Equal("Se procede a realizar el análisis...", informe.Relato);
        Assert.NotNull(informe.PdfPath);
        Assert.Single(fileStorage.ArchivosSubidos);

        var evidencias = dbContext.Evidencias.Where(e => e.InformeId == informeId).ToList();
        Assert.Single(evidencias);
        Assert.Equal("Captura del comercio", evidencias[0].Descripcion);
    }

    [Fact]
    public async Task Rechaza_un_IdRegistro_duplicado_sin_confirmar_reemplazo()
    {
        var (dbContext, caso, dependencia) = await PrepararAsync();
        var fileStorage = new FakeFileStorage();
        var handler = new ConfirmarCargaInformeCommandHandler(dbContext, new FakeCurrentUserService(UsuarioId), fileStorage);

        await handler.Handle(CrearCommand(caso.Id, dependencia.Id), CancellationToken.None);

        await Assert.ThrowsAsync<EntidadDuplicadaException>(() => handler.Handle(
            CrearCommand(caso.Id, dependencia.Id, reemplazar: false), CancellationToken.None));
    }

    [Fact]
    public async Task Reemplaza_el_Informe_existente_y_sus_evidencias_cuando_se_confirma()
    {
        var (dbContext, caso, dependencia) = await PrepararAsync();
        var fileStorage = new FakeFileStorage();
        var handler = new ConfirmarCargaInformeCommandHandler(dbContext, new FakeCurrentUserService(UsuarioId), fileStorage);

        var primeraCargaId = await handler.Handle(CrearCommand(caso.Id, dependencia.Id), CancellationToken.None);

        var segundaCargaId = await handler.Handle(
            CrearCommand(caso.Id, dependencia.Id, reemplazar: true), CancellationToken.None);

        Assert.Equal(primeraCargaId, segundaCargaId);

        var evidencias = dbContext.Evidencias.Where(e => e.InformeId == segundaCargaId).ToList();
        Assert.Single(evidencias); // no se duplicaron
    }

    [Fact]
    public async Task Rechaza_reemplazar_un_Informe_ya_Publicado()
    {
        var dbContext = new TestAppDbContext();
        var caso = new CasoAnalisis(new DateOnly(2026, 7, 21), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var dependencia = new Dependencia("Comisaría 2°", TipoDependencia.Comisaria);
        var informePublicado = new Informe("290/2026", new DateOnly(2026, 7, 14), caso.Id, dependencia.Id, UsuarioId, Guid.NewGuid());
        informePublicado.AgregarFirmante(Guid.NewGuid());
        informePublicado.Publicar();

        dbContext.CasosAnalisis.Add(caso);
        dbContext.Dependencias.Add(dependencia);
        dbContext.Informes.Add(informePublicado);
        await dbContext.SaveChangesAsync();

        var handler = new ConfirmarCargaInformeCommandHandler(dbContext, new FakeCurrentUserService(UsuarioId), new FakeFileStorage());

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            CrearCommand(caso.Id, dependencia.Id, reemplazar: true), CancellationToken.None));
    }

    [Fact]
    public async Task Rechaza_un_Caso_inexistente()
    {
        var dbContext = new TestAppDbContext();
        var dependencia = new Dependencia("Comisaría 2°", TipoDependencia.Comisaria);
        dbContext.Dependencias.Add(dependencia);
        await dbContext.SaveChangesAsync();

        var handler = new ConfirmarCargaInformeCommandHandler(dbContext, new FakeCurrentUserService(UsuarioId), new FakeFileStorage());

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            CrearCommand(Guid.NewGuid(), dependencia.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Persiste_la_Evidencia_con_el_CamaraId_cuando_la_camara_existe()
    {
        var (dbContext, caso, dependencia) = await PrepararAsync();
        var camara = new Camara("SL 18", TipoCamara.Domo, "Av. Illia y San Martín");
        dbContext.Camaras.Add(camara);
        await dbContext.SaveChangesAsync();

        var handler = new ConfirmarCargaInformeCommandHandler(dbContext, new FakeCurrentUserService(UsuarioId), new FakeFileStorage());

        var command = CrearCommand(caso.Id, dependencia.Id) with
        {
            Evidencias = [new(1, camara.Id, null, "Captura del comercio")],
        };

        var informeId = await handler.Handle(command, CancellationToken.None);

        var evidencia = dbContext.Evidencias.Single(e => e.InformeId == informeId);
        Assert.Equal(camara.Id, evidencia.CamaraId);
    }

    [Fact]
    public async Task Rechaza_un_CamaraId_que_no_existe_en_la_base()
    {
        var (dbContext, caso, dependencia) = await PrepararAsync();
        var handler = new ConfirmarCargaInformeCommandHandler(dbContext, new FakeCurrentUserService(UsuarioId), new FakeFileStorage());

        var command = CrearCommand(caso.Id, dependencia.Id) with
        {
            Evidencias = [new(1, Guid.NewGuid(), null, "Captura del comercio")],
        };

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(command, CancellationToken.None));
    }
}
