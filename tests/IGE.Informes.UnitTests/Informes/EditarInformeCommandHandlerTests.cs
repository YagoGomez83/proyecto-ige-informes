using System.Linq;
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
    public async Task EditarInforme_CorrigeFechaAnalisis_PersisteLaFechaNueva()
    {
        var (dbContext, _, _, informe) = await PrepararAsync();
        var handler = new EditarInformeCommandHandler(dbContext, new FakeCurrentUserService(UsuarioId));

        var command = new EditarInformeCommand(
            informe.Id,
            Relato: null,
            DependenciaDestinoId: null,
            CausaCaratula: null,
            CausaNroPiezaSumarial: null,
            CausaCircunscripcionJudicial: null,
            FechaAnalisis: new DateOnly(2022, 8, 9));

        await handler.Handle(command, CancellationToken.None);

        var informeActualizado = await dbContext.Informes.FindAsync(informe.Id);
        Assert.NotNull(informeActualizado);
        Assert.Equal(new DateOnly(2022, 8, 9), informeActualizado.FechaAnalisis);
    }

    [Fact]
    public async Task EditarInforme_SinTocarFechaAnalisis_MantieneLaFechaOriginal()
    {
        var (dbContext, _, _, informe) = await PrepararAsync();
        var fechaOriginal = informe.FechaAnalisis;
        var handler = new EditarInformeCommandHandler(dbContext, new FakeCurrentUserService(UsuarioId));

        var command = new EditarInformeCommand(
            informe.Id,
            Relato: "Solo el relato.",
            DependenciaDestinoId: null,
            CausaCaratula: null,
            CausaNroPiezaSumarial: null,
            CausaCircunscripcionJudicial: null);

        await handler.Handle(command, CancellationToken.None);

        var informeActualizado = await dbContext.Informes.FindAsync(informe.Id);
        Assert.NotNull(informeActualizado);
        Assert.Equal(fechaOriginal, informeActualizado.FechaAnalisis);
    }

    [Fact]
    public void EditarInformeCommandValidator_FechaAnalisisFutura_EsRechazada()
    {
        var validator = new EditarInformeCommandValidator();
        var command = new EditarInformeCommand(
            Guid.NewGuid(),
            Relato: null,
            DependenciaDestinoId: null,
            CausaCaratula: null,
            CausaNroPiezaSumarial: null,
            CausaCircunscripcionJudicial: null,
            FechaAnalisis: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1));

        var resultado = validator.Validate(command);

        Assert.False(resultado.IsValid);
    }

    [Fact]
    public void EditarInformeCommandValidator_SinFechaAnalisis_EsAceptado()
    {
        var validator = new EditarInformeCommandValidator();
        var command = new EditarInformeCommand(
            Guid.NewGuid(),
            Relato: null,
            DependenciaDestinoId: null,
            CausaCaratula: null,
            CausaNroPiezaSumarial: null,
            CausaCircunscripcionJudicial: null);

        var resultado = validator.Validate(command);

        Assert.True(resultado.IsValid);
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

    // HU-02 · Editar / corregir metadatos de un informe (Épica 01),
    // escenarios "Corregir el ID Registro de un informe en Borrador" y
    // "Rechazar un ID Registro que ya existe en otro informe". Requieren:
    // (a) EditarInformeCommand con un parámetro IdRegistro (string?, null =
    // no tocar, mismo criterio que el resto de los campos opcionales del
    // Command); (b) que el Handler llame a Informe.CorregirIdRegistro(...)
    // (ver InformeTests) y antes chequee duplicado contra otros Informes
    // (invariante 2 de docs/03-modelo-dominio.md), mismo patrón que ya usa
    // ConfirmarCargaInformeCommandHandler.

    [Fact]
    public async Task EditarInforme_CorrigeIdRegistroAUnoLibre_PersisteElNuevoIdRegistro()
    {
        var (dbContext, _, _, informe) = await PrepararAsync();
        var handler = new EditarInformeCommandHandler(dbContext, new FakeCurrentUserService(UsuarioId));

        var command = new EditarInformeCommand(
            informe.Id,
            Relato: null,
            DependenciaDestinoId: null,
            CausaCaratula: null,
            CausaNroPiezaSumarial: null,
            CausaCircunscripcionJudicial: null,
            IdRegistro: "101/2022");

        await handler.Handle(command, CancellationToken.None);

        var informeActualizado = await dbContext.Informes.FindAsync(informe.Id);
        Assert.NotNull(informeActualizado);
        Assert.Equal("101/2022", informeActualizado.IdRegistro);
    }

    [Fact]
    public async Task EditarInforme_SinTocarIdRegistro_MantieneElIdRegistroOriginal()
    {
        var (dbContext, _, _, informe) = await PrepararAsync();
        var idRegistroOriginal = informe.IdRegistro;
        var handler = new EditarInformeCommandHandler(dbContext, new FakeCurrentUserService(UsuarioId));

        var command = new EditarInformeCommand(
            informe.Id,
            Relato: "Solo el relato.",
            DependenciaDestinoId: null,
            CausaCaratula: null,
            CausaNroPiezaSumarial: null,
            CausaCircunscripcionJudicial: null);

        await handler.Handle(command, CancellationToken.None);

        var informeActualizado = await dbContext.Informes.FindAsync(informe.Id);
        Assert.NotNull(informeActualizado);
        Assert.Equal(idRegistroOriginal, informeActualizado.IdRegistro);
    }

    [Fact]
    public async Task EditarInforme_IdRegistroYaExisteEnOtroInforme_RechazaConEntidadDuplicadaException()
    {
        var (dbContext, caso, dependenciaOriginal, informe) = await PrepararAsync();
        var otroInforme = new Informe(
            "101/2022",
            new DateOnly(2022, 1, 5),
            caso.Id,
            dependenciaOriginal.Id,
            UsuarioId);
        dbContext.Informes.Add(otroInforme);
        await dbContext.SaveChangesAsync();

        var handler = new EditarInformeCommandHandler(dbContext, new FakeCurrentUserService(UsuarioId));

        var command = new EditarInformeCommand(
            informe.Id,
            Relato: null,
            DependenciaDestinoId: null,
            CausaCaratula: null,
            CausaNroPiezaSumarial: null,
            CausaCircunscripcionJudicial: null,
            IdRegistro: "101/2022");

        await Assert.ThrowsAsync<EntidadDuplicadaException>(() => handler.Handle(command, CancellationToken.None));

        var informeSinTocar = await dbContext.Informes.FindAsync(informe.Id);
        Assert.NotNull(informeSinTocar);
        Assert.Equal("290/2026", informeSinTocar.IdRegistro);
    }

    [Fact]
    public async Task EditarInforme_CorrigeIdRegistroAlMismoValorQueYaTiene_NoLoConsideraDuplicadoConsigoMismo()
    {
        // El chequeo de duplicado debe excluir al propio Informe que se está
        // editando — corregir el ID Registro a su mismo valor actual (o
        // volver a enviar el mismo dato sin cambiarlo) no debe explotar
        // contra sí mismo.
        var (dbContext, _, _, informe) = await PrepararAsync();
        var handler = new EditarInformeCommandHandler(dbContext, new FakeCurrentUserService(UsuarioId));

        var command = new EditarInformeCommand(
            informe.Id,
            Relato: null,
            DependenciaDestinoId: null,
            CausaCaratula: null,
            CausaNroPiezaSumarial: null,
            CausaCircunscripcionJudicial: null,
            IdRegistro: informe.IdRegistro);

        await handler.Handle(command, CancellationToken.None);

        var informeActualizado = await dbContext.Informes.FindAsync(informe.Id);
        Assert.NotNull(informeActualizado);
        Assert.Equal(informe.IdRegistro, informeActualizado.IdRegistro);
    }

    // HU-02 · escenario "Vincular la Causa a una ya existente por Pieza
    // Sumarial": el Handler todavía siempre crea una Causa nueva (ver
    // EditarInforme_CorreccionDeCamposEnBorrador_PersisteRelatoDependenciaYCausaNuevos
    // más arriba, que espera justamente eso) — falta el matching por
    // NroPiezaSumarial exacto contra Causas ya existentes descripto en
    // docs/03-modelo-dominio.md, "Decisiones ya resueltas".

    [Fact]
    public async Task EditarInforme_PiezaSumarialCoincideExactoConCausaExistente_ReusaLaCausaExistenteSinCrearUnaNueva()
    {
        var (dbContext, _, _, informe) = await PrepararAsync();
        // NroPiezaSumarial distinto del que ya usa PrepararAsync() ("7070029/26",
        // ver causaOriginal) — si coincidieran, ambas Causas matchean el
        // mismo número (no hay índice único en NroPiezaSumarial) y el
        // resultado de FirstOrDefaultAsync sería ambiguo.
        var causaExistente = new Causa("N.N. s/Hurto", "8080099/26", "Primera Circunscripción");
        dbContext.Causas.Add(causaExistente);
        await dbContext.SaveChangesAsync();
        var cantidadCausasAntes = dbContext.Causas.Count();

        // Uso otro Informe (sin Causa asignada) para no pisar la Causa que
        // ya trae el Informe de PrepararAsync().
        var informeSinCausa = new Informe(
            "291/2026",
            new DateOnly(2026, 7, 22),
            informe.CasoAnalisisId!.Value,
            informe.DependenciaDestinoId,
            UsuarioId);
        dbContext.Informes.Add(informeSinCausa);
        await dbContext.SaveChangesAsync();

        var handler = new EditarInformeCommandHandler(dbContext, new FakeCurrentUserService(UsuarioId));

        var command = new EditarInformeCommand(
            informeSinCausa.Id,
            Relato: null,
            DependenciaDestinoId: null,
            CausaCaratula: "N.N. s/Hurto",
            CausaNroPiezaSumarial: "8080099/26",
            CausaCircunscripcionJudicial: "Primera Circunscripción");

        await handler.Handle(command, CancellationToken.None);

        var informeActualizado = await dbContext.Informes.FindAsync(informeSinCausa.Id);
        Assert.NotNull(informeActualizado);
        Assert.Equal(causaExistente.Id, informeActualizado.CausaId);
        Assert.Equal(cantidadCausasAntes, dbContext.Causas.Count());
    }

    [Fact]
    public async Task EditarInforme_PiezaSumarialSinCoincidenciaExacta_CreaUnaCausaNueva()
    {
        var (dbContext, _, _, informe) = await PrepararAsync();
        var cantidadCausasAntes = dbContext.Causas.Count();
        var handler = new EditarInformeCommandHandler(dbContext, new FakeCurrentUserService(UsuarioId));

        var command = new EditarInformeCommand(
            informe.Id,
            Relato: null,
            DependenciaDestinoId: null,
            CausaCaratula: "AV. INFRACCION LEY 23.737",
            CausaNroPiezaSumarial: "9999999/26",
            CausaCircunscripcionJudicial: "Segunda Circunscripción");

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal(cantidadCausasAntes + 1, dbContext.Causas.Count());
    }

    // HU-02 · escenario "Completar la Causa sin Circunscripción judicial al
    // editar": con Carátula y N° de Pieza Sumarial completos alcanza para
    // crear/vincular la Causa — la condición del Handler para tocar la
    // Causa pasa de exigir los 3 campos a exigir solo esos 2 (ver
    // docs/03-modelo-dominio.md, "Decisiones ya resueltas").
    [Fact]
    public async Task EditarInforme_CompletaCausaSinCircunscripcionJudicial_CreaOVinculaLaCausaIgual()
    {
        var (dbContext, _, _, informe) = await PrepararAsync();
        var handler = new EditarInformeCommandHandler(dbContext, new FakeCurrentUserService(UsuarioId));

        var command = new EditarInformeCommand(
            informe.Id,
            Relato: null,
            DependenciaDestinoId: null,
            CausaCaratula: "AV. INFRACCION LEY 23.737",
            CausaNroPiezaSumarial: "7070099/26",
            CausaCircunscripcionJudicial: null);

        await handler.Handle(command, CancellationToken.None);

        var informeActualizado = await dbContext.Informes.FindAsync(informe.Id);
        Assert.NotNull(informeActualizado);
        Assert.NotNull(informeActualizado.CausaId);

        var causaActualizada = await dbContext.Causas.FindAsync(informeActualizado.CausaId);
        Assert.NotNull(causaActualizada);
        Assert.Equal("AV. INFRACCION LEY 23.737", causaActualizada.Caratula);
        Assert.Equal("7070099/26", causaActualizada.NroPiezaSumarial);
        Assert.Null(causaActualizada.CircunscripcionJudicial);
    }
}
