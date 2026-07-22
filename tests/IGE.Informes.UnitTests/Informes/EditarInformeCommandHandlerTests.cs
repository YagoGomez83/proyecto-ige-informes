using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Informes.Commands.EditarInforme;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Informes;

/// <summary>
/// HU-02 · Editar / corregir metadatos de un informe (Épica 01).
///
/// Alcance de esta primera pasada (confirmado): Relato, DependenciaDestinoId
/// y los 3 campos de Causa (Caratula, NroPiezaSumarial,
/// CircunscripcionJudicial). Vehículos/Personas de Evidencias quedan
/// explícitamente fuera — deuda para otra HU.
///
/// La auditoría de la edición (AuditLog con usuario y fecha/hora) se
/// verifica a nivel de integración contra Postgres real con
/// AuditLogInterceptor — ver
/// tests/IGE.Informes.IntegrationTests/Informes/EditarInformeAuditLogTests.cs
/// — porque TestAppDbContext (InMemory) no tiene el interceptor registrado,
/// igual que ConfirmarCargaInformeCommandHandlerTests no lo verifica ahí.
/// </summary>
public class EditarInformeCommandHandlerTests
{
    private static readonly Guid UsuarioId = Guid.NewGuid();

    private static async Task<(TestAppDbContext DbContext, CasoAnalisis Caso, Dependencia DependenciaOriginal, Informe Informe)> PrepararAsync()
    {
        var dbContext = new TestAppDbContext();
        var caso = new CasoAnalisis(new DateOnly(2026, 7, 21), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var dependenciaOriginal = new Dependencia("Comisaría 2°", TipoDependencia.Comisaria);
        var causaOriginal = new Causa("N.N. s/Robo", "7070029/26", "Primera Circunscripción");

        dbContext.CasosAnalisis.Add(caso);
        dbContext.Dependencias.Add(dependenciaOriginal);
        dbContext.Causas.Add(causaOriginal);
        await dbContext.SaveChangesAsync();

        var informe = new Informe(
            "290/2026",
            new DateOnly(2026, 7, 14),
            caso.Id,
            dependenciaOriginal.Id,
            UsuarioId,
            causaOriginal.Id);
        informe.CompletarRelato("Relato original extraído del PDF.");

        dbContext.Informes.Add(informe);
        await dbContext.SaveChangesAsync();

        return (dbContext, caso, dependenciaOriginal, informe);
    }

    [Fact]
    public async Task EditarInforme_CorreccionDeCamposEnBorrador_PersisteRelatoDependenciaYCausaNuevos()
    {
        var (dbContext, _, _, informe) = await PrepararAsync();
        var nuevaDependencia = new Dependencia("Comisaría 5°", TipoDependencia.Comisaria);
        dbContext.Dependencias.Add(nuevaDependencia);
        await dbContext.SaveChangesAsync();

        var handler = new EditarInformeCommandHandler(dbContext, new FakeCurrentUserService(UsuarioId));

        var command = new EditarInformeCommand(
            informe.Id,
            Relato: "Relato corregido manualmente por el Analista.",
            DependenciaDestinoId: nuevaDependencia.Id,
            CausaCaratula: "AV. INFRACCION LEY 23.737",
            CausaNroPiezaSumarial: "7070099/26",
            CausaCircunscripcionJudicial: "Segunda Circunscripción");

        await handler.Handle(command, CancellationToken.None);

        var informeActualizado = await dbContext.Informes.FindAsync(informe.Id);
        Assert.NotNull(informeActualizado);
        Assert.Equal("Relato corregido manualmente por el Analista.", informeActualizado.Relato);
        Assert.Equal(nuevaDependencia.Id, informeActualizado.DependenciaDestinoId);
        Assert.NotNull(informeActualizado.CausaId);

        var causaActualizada = await dbContext.Causas.FindAsync(informeActualizado.CausaId);
        Assert.NotNull(causaActualizada);
        Assert.Equal("AV. INFRACCION LEY 23.737", causaActualizada.Caratula);
        Assert.Equal("7070099/26", causaActualizada.NroPiezaSumarial);
        Assert.Equal("Segunda Circunscripción", causaActualizada.CircunscripcionJudicial);
    }

    [Fact]
    public async Task EditarInforme_SoloRelatoSinTocarCausaNiDependencia_MantieneLosDemasCamposSinCambios()
    {
        var (dbContext, _, dependenciaOriginal, informe) = await PrepararAsync();
        var causaOriginalId = informe.CausaId;

        var handler = new EditarInformeCommandHandler(dbContext, new FakeCurrentUserService(UsuarioId));

        var command = new EditarInformeCommand(
            informe.Id,
            Relato: "Solo corrijo el relato, nada más.",
            DependenciaDestinoId: null,
            CausaCaratula: null,
            CausaNroPiezaSumarial: null,
            CausaCircunscripcionJudicial: null);

        await handler.Handle(command, CancellationToken.None);

        var informeActualizado = await dbContext.Informes.FindAsync(informe.Id);
        Assert.NotNull(informeActualizado);
        Assert.Equal("Solo corrijo el relato, nada más.", informeActualizado.Relato);
        Assert.Equal(dependenciaOriginal.Id, informeActualizado.DependenciaDestinoId);
        Assert.Equal(causaOriginalId, informeActualizado.CausaId);
    }

    [Fact]
    public async Task EditarInforme_InformePublicado_RechazaLaEdicionConInvalidOperationException()
    {
        var dbContext = new TestAppDbContext();
        var caso = new CasoAnalisis(new DateOnly(2026, 7, 21), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var dependencia = new Dependencia("Comisaría 2°", TipoDependencia.Comisaria);
        var causa = new Causa("N.N. s/Robo", "7070029/26", "Primera Circunscripción");

        dbContext.CasosAnalisis.Add(caso);
        dbContext.Dependencias.Add(dependencia);
        dbContext.Causas.Add(causa);
        await dbContext.SaveChangesAsync();

        var informePublicado = new Informe("290/2026", new DateOnly(2026, 7, 14), caso.Id, dependencia.Id, UsuarioId, causa.Id);
        informePublicado.AgregarFirmante(Guid.NewGuid());
        informePublicado.Publicar();

        dbContext.Informes.Add(informePublicado);
        await dbContext.SaveChangesAsync();

        var handler = new EditarInformeCommandHandler(dbContext, new FakeCurrentUserService(UsuarioId));

        var command = new EditarInformeCommand(
            informePublicado.Id,
            Relato: "Intento de corrección post-publicación.",
            DependenciaDestinoId: null,
            CausaCaratula: null,
            CausaNroPiezaSumarial: null,
            CausaCircunscripcionJudicial: null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task EditarInforme_InformeInexistente_RechazaConEntidadNoEncontradaException()
    {
        var dbContext = new TestAppDbContext();
        var handler = new EditarInformeCommandHandler(dbContext, new FakeCurrentUserService(UsuarioId));

        var idInexistente = Guid.NewGuid();
        var command = new EditarInformeCommand(
            idInexistente,
            Relato: "No debería aplicarse nunca.",
            DependenciaDestinoId: null,
            CausaCaratula: null,
            CausaNroPiezaSumarial: null,
            CausaCircunscripcionJudicial: null);

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task EditarInforme_NuevaDependenciaDestinoInexistente_RechazaConEntidadNoEncontradaException()
    {
        var (dbContext, _, _, informe) = await PrepararAsync();
        var handler = new EditarInformeCommandHandler(dbContext, new FakeCurrentUserService(UsuarioId));

        var dependenciaIdInexistente = Guid.NewGuid();
        var command = new EditarInformeCommand(
            informe.Id,
            Relato: null,
            DependenciaDestinoId: dependenciaIdInexistente,
            CausaCaratula: null,
            CausaNroPiezaSumarial: null,
            CausaCircunscripcionJudicial: null);

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(command, CancellationToken.None));
    }
}
