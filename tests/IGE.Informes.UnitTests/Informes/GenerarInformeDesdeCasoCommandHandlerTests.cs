using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Informes.Commands.GenerarInformeDesdeCaso;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Informes;

public class GenerarInformeDesdeCasoCommandHandlerTests
{
    private static readonly Guid UsuarioId = Guid.NewGuid();

    private static async Task<(TestAppDbContext DbContext, CasoAnalisis Caso, Dependencia Dependencia)> PrepararAsync()
    {
        var dbContext = new TestAppDbContext();
        var caso = new CasoAnalisis(new DateOnly(2026, 7, 21), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var dependencia = new Dependencia("Fiscalía N°3", TipoDependencia.Fiscalia);

        dbContext.CasosAnalisis.Add(caso);
        dbContext.Dependencias.Add(dependencia);
        await dbContext.SaveChangesAsync();

        return (dbContext, caso, dependencia);
    }

    [Fact]
    public async Task Genera_el_Informe_en_Borrador_con_Causa_y_el_solicitante_como_Interviniente()
    {
        var (dbContext, caso, dependencia) = await PrepararAsync();
        var handler = new GenerarInformeDesdeCasoCommandHandler(dbContext, new FakeCurrentUserService(UsuarioId));

        var informeId = await handler.Handle(
            new GenerarInformeDesdeCasoCommand(caso.Id, dependencia.Id, "N.N. s/Robo", "7070029/26", "Primera Circunscripción"),
            CancellationToken.None);

        var informe = await dbContext.Informes.FindAsync(informeId);
        Assert.NotNull(informe);
        Assert.Equal(EstadoInforme.Borrador, informe.Estado);
        Assert.Equal(caso.Id, informe.CasoAnalisisId);
        Assert.Equal(dependencia.Id, informe.DependenciaDestinoId);
        Assert.NotNull(informe.CausaId);
        Assert.Equal(UsuarioId, informe.Analistas.Single().UsuarioId);
        Assert.Equal(RolInformeAnalista.Interviniente, informe.Analistas.Single().Rol);
    }

    [Fact]
    public async Task El_primer_Informe_del_anio_obtiene_correlativo_1()
    {
        var (dbContext, caso, dependencia) = await PrepararAsync();
        var handler = new GenerarInformeDesdeCasoCommandHandler(dbContext, new FakeCurrentUserService(UsuarioId));

        var informeId = await handler.Handle(
            new GenerarInformeDesdeCasoCommand(caso.Id, dependencia.Id, "N.N. s/Robo", "7070029/26", "Primera Circunscripción"),
            CancellationToken.None);

        var informe = await dbContext.Informes.FindAsync(informeId);
        Assert.Equal($"1/{DateTime.UtcNow.Year}", informe!.IdRegistro);
    }

    [Fact]
    public async Task Un_segundo_Informe_sobre_el_mismo_Caso_obtiene_el_siguiente_correlativo()
    {
        var (dbContext, caso, dependencia) = await PrepararAsync();
        var otraDependencia = new Dependencia("Comisaría 2°", TipoDependencia.Comisaria);
        dbContext.Dependencias.Add(otraDependencia);
        await dbContext.SaveChangesAsync();

        var handler = new GenerarInformeDesdeCasoCommandHandler(dbContext, new FakeCurrentUserService(UsuarioId));

        await handler.Handle(
            new GenerarInformeDesdeCasoCommand(caso.Id, dependencia.Id, "N.N. s/Robo", "7070029/26", "Primera Circunscripción"),
            CancellationToken.None);

        var segundoInformeId = await handler.Handle(
            new GenerarInformeDesdeCasoCommand(caso.Id, otraDependencia.Id, "N.N. s/Robo (2)", "7070030/26", "Primera Circunscripción"),
            CancellationToken.None);

        var segundoInforme = await dbContext.Informes.FindAsync(segundoInformeId);
        Assert.Equal($"2/{DateTime.UtcNow.Year}", segundoInforme!.IdRegistro);
    }

    [Fact]
    public async Task Genera_el_Informe_con_Causa_sin_Circunscripcion_judicial()
    {
        var (dbContext, caso, dependencia) = await PrepararAsync();
        var handler = new GenerarInformeDesdeCasoCommandHandler(dbContext, new FakeCurrentUserService(UsuarioId));

        var informeId = await handler.Handle(
            new GenerarInformeDesdeCasoCommand(caso.Id, dependencia.Id, "N.N. s/Robo", "7070029/26", null),
            CancellationToken.None);

        var informe = await dbContext.Informes.FindAsync(informeId);
        Assert.NotNull(informe);
        Assert.NotNull(informe.CausaId);

        var causa = await dbContext.Causas.FindAsync(informe.CausaId);
        Assert.NotNull(causa);
        Assert.Equal("N.N. s/Robo", causa.Caratula);
        Assert.Null(causa.CircunscripcionJudicial);
    }

    [Fact]
    public async Task Rechaza_un_Caso_inexistente()
    {
        var dbContext = new TestAppDbContext();
        var dependencia = new Dependencia("Fiscalía N°3", TipoDependencia.Fiscalia);
        dbContext.Dependencias.Add(dependencia);
        await dbContext.SaveChangesAsync();

        var handler = new GenerarInformeDesdeCasoCommandHandler(dbContext, new FakeCurrentUserService(UsuarioId));

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            new GenerarInformeDesdeCasoCommand(Guid.NewGuid(), dependencia.Id, "N.N.", "123/26", "Circunscripción"),
            CancellationToken.None));
    }

    [Fact]
    public async Task Rechaza_una_Dependencia_destino_inexistente()
    {
        var (dbContext, caso, _) = await PrepararAsync();
        var handler = new GenerarInformeDesdeCasoCommandHandler(dbContext, new FakeCurrentUserService(UsuarioId));

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            new GenerarInformeDesdeCasoCommand(caso.Id, Guid.NewGuid(), "N.N.", "123/26", "Circunscripción"),
            CancellationToken.None));
    }

    [Fact]
    public async Task Rechaza_ejecutar_sin_usuario_autenticado()
    {
        var (dbContext, caso, dependencia) = await PrepararAsync();
        var handler = new GenerarInformeDesdeCasoCommandHandler(dbContext, new FakeCurrentUserService(usuarioId: null));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => handler.Handle(
            new GenerarInformeDesdeCasoCommand(caso.Id, dependencia.Id, "N.N.", "123/26", "Circunscripción"),
            CancellationToken.None));
    }
}
