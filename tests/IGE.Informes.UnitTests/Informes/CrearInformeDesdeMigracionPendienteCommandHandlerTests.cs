using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Application.Common.Security;
using IGE.Informes.Application.Informes.Commands.CrearInformeDesdeMigracionPendiente;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Informes;

/// <summary>
/// HU-04 · Migración histórica de informes desde Drive
/// (docs/epic-01-gestion-informes.md) — Característica "Migración masiva",
/// escenarios "Completar la Fecha de Análisis de una Migración Pendiente",
/// "Completar el ID Registro de una Migración Pendiente" y "El ID Registro
/// ingresado ya existe". El Command ahora recibe además
/// <c>string? IdRegistro</c> (el que tipea el Administrador cuando la
/// MigracionPendiente no lo tenía). Estos tests están escritos antes de la
/// implementación (TDD), por lo que deben fallar en rojo hasta que se
/// actualice
/// IGE.Informes.Application.Informes.Commands.CrearInformeDesdeMigracionPendiente.*
/// — pantalla Admin /informes/migrar/pendientes.
/// </summary>
public class CrearInformeDesdeMigracionPendienteCommandHandlerTests
{
    private static readonly Guid UsuarioMigradorOriginalId = Guid.NewGuid();
    private static readonly Guid UsuarioAdminQueCompletaId = Guid.NewGuid();

    private static async Task<(TestAppDbContext DbContext, Dependencia Dependencia, MigracionPendiente MigracionPendiente)> PrepararAsync(
        string? idRegistro = "700/2022",
        string? causaCaratula = "AV. INFRACCION LEY 23.737",
        string? piezaSumarial = "7070029/26",
        string? relato = "Se procede a realizar el análisis histórico...")
    {
        var dbContext = new TestAppDbContext();
        var dependencia = new Dependencia("Comisaría 2°", TipoDependencia.Comisaria);
        dbContext.Dependencias.Add(dependencia);
        await dbContext.SaveChangesAsync();

        var migracionPendiente = new MigracionPendiente(
            idRegistro,
            "migraciones-pendientes/700-2022.pdf",
            dependencia.Id,
            UsuarioMigradorOriginalId,
            causaCaratula,
            piezaSumarial,
            relato);

        dbContext.MigracionesPendientes.Add(migracionPendiente);
        await dbContext.SaveChangesAsync();

        return (dbContext, dependencia, migracionPendiente);
    }

    private static CrearInformeDesdeMigracionPendienteCommandHandler CrearHandler(TestAppDbContext dbContext, IAuditLogger? auditLogger = null) =>
        new(dbContext, new FakeCurrentUserService(UsuarioAdminQueCompletaId, Roles.Admin), auditLogger ?? new FakeAuditLogger());

    [Fact]
    public async Task CompletarMigracionPendiente_ConFechaValida_CreaElInformeRealConLosDatosYaExtraidosMasLaFecha()
    {
        var (dbContext, dependencia, migracionPendiente) = await PrepararAsync();
        var handler = CrearHandler(dbContext);
        var fechaIngresada = new DateOnly(2022, 6, 15);

        var command = new CrearInformeDesdeMigracionPendienteCommand(migracionPendiente.Id, fechaIngresada, null);

        var informeId = await handler.Handle(command, CancellationToken.None);

        var informe = await dbContext.Informes.FindAsync(informeId);
        Assert.NotNull(informe);
        Assert.Equal("700/2022", informe.IdRegistro);
        Assert.Equal(fechaIngresada, informe.FechaAnalisis);
        Assert.Equal(dependencia.Id, informe.DependenciaDestinoId);
        Assert.Equal(OrigenInforme.Migrado, informe.Origen);
        Assert.Null(informe.CasoAnalisisId);
        Assert.Equal(EstadoInforme.Borrador, informe.Estado);
        Assert.Equal("Se procede a realizar el análisis histórico...", informe.Relato);
    }

    [Fact]
    public async Task CompletarMigracionPendiente_ConFechaValida_LaMigracionPendienteDejaDeListarse()
    {
        var (dbContext, _, migracionPendiente) = await PrepararAsync();
        var handler = CrearHandler(dbContext);

        var command = new CrearInformeDesdeMigracionPendienteCommand(migracionPendiente.Id, new DateOnly(2022, 6, 15), null);

        await handler.Handle(command, CancellationToken.None);

        Assert.Empty(dbContext.MigracionesPendientes.ToList());
    }

    [Fact]
    public async Task CompletarMigracionPendiente_CausaExtraidaCoincideConCausaExistente_VinculaAutomaticamenteSinCrearUnaNueva()
    {
        var (dbContext, _, migracionPendiente) = await PrepararAsync(causaCaratula: "AV. INFRACCION LEY 23.737", piezaSumarial: "7070029/26");
        var causaExistente = new Causa("AV. INFRACCION LEY 23.737", "7070029/26", "Primera Circunscripción");
        dbContext.Causas.Add(causaExistente);
        await dbContext.SaveChangesAsync();
        var cantidadCausasAntes = dbContext.Causas.Count();

        var auditLogger = new FakeAuditLogger();
        var handler = CrearHandler(dbContext, auditLogger);
        var command = new CrearInformeDesdeMigracionPendienteCommand(migracionPendiente.Id, new DateOnly(2022, 6, 15), null);

        var informeId = await handler.Handle(command, CancellationToken.None);

        var informe = await dbContext.Informes.FindAsync(informeId);
        Assert.NotNull(informe);
        Assert.Equal(causaExistente.Id, informe.CausaId);
        Assert.Equal(cantidadCausasAntes, dbContext.Causas.Count());

        // Ningún humano vio/confirmó esta vinculación en el momento — se
        // audita aparte para poder revisarla después (hallazgo del
        // security-reviewer).
        Assert.Contains(auditLogger.Registros, r => r.Accion == "CausaAutoAsignadaMigracion" && r.EntidadId == informeId);
    }

    [Fact]
    public async Task CompletarMigracionPendiente_CausaExtraidaSinCoincidenciaExacta_InformeQuedaSinCausaAsociada()
    {
        var (dbContext, _, migracionPendiente) = await PrepararAsync(causaCaratula: "AV. INFRACCION LEY 23.737", piezaSumarial: "7070029/26");
        var auditLogger = new FakeAuditLogger();
        var handler = CrearHandler(dbContext, auditLogger);
        var command = new CrearInformeDesdeMigracionPendienteCommand(migracionPendiente.Id, new DateOnly(2022, 6, 15), null);

        var informeId = await handler.Handle(command, CancellationToken.None);

        var informe = await dbContext.Informes.FindAsync(informeId);
        Assert.NotNull(informe);
        Assert.Null(informe.CausaId);
        Assert.DoesNotContain(auditLogger.Registros, r => r.Accion == "CausaAutoAsignadaMigracion");
        Assert.Empty(dbContext.Causas.ToList());
    }

    [Fact]
    public async Task CompletarMigracionPendiente_MigracionPendienteInexistente_RechazaConEntidadNoEncontrada()
    {
        var dbContext = new TestAppDbContext();
        var handler = CrearHandler(dbContext);

        var command = new CrearInformeDesdeMigracionPendienteCommand(Guid.NewGuid(), new DateOnly(2022, 6, 15), null);

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CompletarMigracionPendiente_IdRegistroYaExisteComoInformeReal_RechazaConEntidadDuplicada()
    {
        // Puede pasar si el mismo PDF se cargó individualmente (HU-01)
        // mientras la MigracionPendiente seguía sin completar.
        var (dbContext, dependencia, migracionPendiente) = await PrepararAsync(idRegistro: "800/2022");
        var informeYaExistente = Informe.CrearMigrado("800/2022", new DateOnly(2022, 1, 1), dependencia.Id, Guid.NewGuid());
        dbContext.Informes.Add(informeYaExistente);
        await dbContext.SaveChangesAsync();

        var handler = CrearHandler(dbContext);
        var command = new CrearInformeDesdeMigracionPendienteCommand(migracionPendiente.Id, new DateOnly(2022, 6, 15), null);

        await Assert.ThrowsAsync<EntidadDuplicadaException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CompletarMigracionPendiente_ConCausaIdElegidaPorElUsuario_VinculaEsaCausaSinCrearUnaNueva()
    {
        // Carátula extraída ("AV. HURTO CALIFICADO") no matchea exacto por
        // Pieza Sumarial contra ninguna Causa existente — el usuario eligió
        // a mano una Causa parecida sugerida por SugerirCausasQuery en la UI.
        var (dbContext, _, migracionPendiente) = await PrepararAsync(causaCaratula: "AV. HURTO CALIFICADO", piezaSumarial: "111/2023");
        var causaElegida = new Causa("AV.HURTO CALIFICADO", "110/2023", "Primera Circunscripción");
        dbContext.Causas.Add(causaElegida);
        await dbContext.SaveChangesAsync();
        var cantidadCausasAntes = dbContext.Causas.Count();

        var auditLogger = new FakeAuditLogger();
        var handler = CrearHandler(dbContext, auditLogger);
        var command = new CrearInformeDesdeMigracionPendienteCommand(migracionPendiente.Id, new DateOnly(2023, 8, 8), null, causaElegida.Id);

        var informeId = await handler.Handle(command, CancellationToken.None);

        var informe = await dbContext.Informes.FindAsync(informeId);
        Assert.NotNull(informe);
        Assert.Equal(causaElegida.Id, informe.CausaId);
        Assert.Equal(cantidadCausasAntes, dbContext.Causas.Count());

        // A diferencia del auto-match silencioso, una Causa elegida a mano
        // por el usuario ya tuvo revisión humana — no debe marcarse como
        // "auto-asignada" en el reporte de revisión posterior.
        Assert.DoesNotContain(auditLogger.Registros, r => r.Accion == "CausaAutoAsignadaMigracion");
    }

    [Fact]
    public async Task CompletarMigracionPendiente_ConCausaIdInexistente_RechazaConEntidadNoEncontrada()
    {
        var (dbContext, _, migracionPendiente) = await PrepararAsync();
        var handler = CrearHandler(dbContext);
        var command = new CrearInformeDesdeMigracionPendienteCommand(migracionPendiente.Id, new DateOnly(2022, 6, 15), null, Guid.NewGuid());

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CompletarMigracionPendiente_SinCausaExtraidaPorElParser_CreaElInformeSinCausaAsociada()
    {
        var (dbContext, _, migracionPendiente) = await PrepararAsync(causaCaratula: null, piezaSumarial: null);
        var handler = CrearHandler(dbContext);

        var command = new CrearInformeDesdeMigracionPendienteCommand(migracionPendiente.Id, new DateOnly(2022, 6, 15), null);

        var informeId = await handler.Handle(command, CancellationToken.None);

        var informe = await dbContext.Informes.FindAsync(informeId);
        Assert.NotNull(informe);
        Assert.Null(informe.CausaId);
    }

    // HU-04 · Migración histórica de informes desde Drive
    // (docs/epic-01-gestion-informes.md), extensión (2026-08-12): se abre a
    // los 3 roles (Analista, Supervisor, Admin) — ya no es exclusiva de
    // Admin. Reemplaza al viejo
    // "CrearInformeDesdeMigracionPendienteCommand_DeclaraAutorizacion_SoloParaRolAdmin"
    // y debe fallar en rojo hasta que se actualice el atributo [Autorizar].
    [Fact]
    public void CrearInformeDesdeMigracionPendienteCommand_DeclaraAutorizacion_ParaAnalistaSupervisorYAdmin()
    {
        var atributo = typeof(CrearInformeDesdeMigracionPendienteCommand)
            .GetCustomAttributes(typeof(AutorizarAttribute), inherit: true)
            .Cast<AutorizarAttribute>()
            .SingleOrDefault();

        Assert.NotNull(atributo);
        Assert.Equal(3, atributo.Roles.Count);
        Assert.Contains(Roles.Analista, atributo.Roles);
        Assert.Contains(Roles.Supervisor, atributo.Roles);
        Assert.Contains(Roles.Admin, atributo.Roles);
    }

    [Fact]
    public async Task CompletarMigracionPendiente_RegistraLaCreacionDelInformeEnAuditLog()
    {
        var (dbContext, _, migracionPendiente) = await PrepararAsync();
        var auditLogger = new FakeAuditLogger();
        var handler = CrearHandler(dbContext, auditLogger);

        var command = new CrearInformeDesdeMigracionPendienteCommand(migracionPendiente.Id, new DateOnly(2022, 6, 15), null);

        await handler.Handle(command, CancellationToken.None);

        Assert.Contains(auditLogger.Registros, r => r.Entidad == nameof(Informe));
    }

    // Escenario Gherkin "Completar el ID Registro de una Migración
    // Pendiente": la MigracionPendiente no tenía IdRegistro reconocido
    // (IdRegistro = null) — el Administrador lo ingresa junto con la Fecha
    // de Análisis (si también faltaba) desde /informes/migrar/pendientes.

    [Fact]
    public async Task CompletarMigracionPendiente_SinIdRegistroYConIdRegistroInformadoEnElCommand_CreaElInformeConEseIdRegistro()
    {
        var (dbContext, dependencia, migracionPendiente) = await PrepararAsync(idRegistro: null, relato: "Relato sin ID Registro reconocido");
        var handler = CrearHandler(dbContext);

        var command = new CrearInformeDesdeMigracionPendienteCommand(migracionPendiente.Id, new DateOnly(2022, 6, 15), "900/2022");

        var informeId = await handler.Handle(command, CancellationToken.None);

        var informe = await dbContext.Informes.FindAsync(informeId);
        Assert.NotNull(informe);
        Assert.Equal("900/2022", informe.IdRegistro);
        Assert.Equal(new DateOnly(2022, 6, 15), informe.FechaAnalisis);
        Assert.Equal(dependencia.Id, informe.DependenciaDestinoId);
        Assert.Equal(OrigenInforme.Migrado, informe.Origen);
        Assert.Equal("Relato sin ID Registro reconocido", informe.Relato);
    }

    [Fact]
    public async Task CompletarMigracionPendiente_SinIdRegistroYConIdRegistroInformado_LaMigracionPendienteDejaDeListarse()
    {
        var (dbContext, _, migracionPendiente) = await PrepararAsync(idRegistro: null);
        var handler = CrearHandler(dbContext);

        var command = new CrearInformeDesdeMigracionPendienteCommand(migracionPendiente.Id, new DateOnly(2022, 6, 15), "901/2022");

        await handler.Handle(command, CancellationToken.None);

        Assert.Empty(dbContext.MigracionesPendientes.ToList());
    }

    [Fact]
    public async Task CompletarMigracionPendiente_SinIdRegistroYSinIdRegistroInformadoEnElCommand_Rechaza()
    {
        // El diseño exige explícitamente: "si la MigracionPendiente.IdRegistro
        // es null, el Command DEBE traer un IdRegistro no vacío o el Handler
        // debe rechazar". El Validator no puede saber esto sin IO (no sabe
        // si la MigracionPendiente en base tenía o no IdRegistro), así que
        // esta regla vive en el Handler.
        var (dbContext, _, migracionPendiente) = await PrepararAsync(idRegistro: null);
        var handler = CrearHandler(dbContext);

        var command = new CrearInformeDesdeMigracionPendienteCommand(migracionPendiente.Id, new DateOnly(2022, 6, 15), null);

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CompletarMigracionPendiente_SinIdRegistroYSinIdRegistroInformado_NoBorraLaMigracionPendiente()
    {
        var (dbContext, _, migracionPendiente) = await PrepararAsync(idRegistro: null);
        var handler = CrearHandler(dbContext);

        var command = new CrearInformeDesdeMigracionPendienteCommand(migracionPendiente.Id, new DateOnly(2022, 6, 15), null);

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(command, CancellationToken.None));

        Assert.Single(dbContext.MigracionesPendientes.ToList());
    }

    // Escenario Gherkin "El ID Registro ingresado ya existe".

    [Fact]
    public async Task CompletarMigracionPendiente_SinIdRegistroYElIdRegistroIngresadoYaExisteComoInformeReal_RechazaConEntidadDuplicada()
    {
        var (dbContext, dependencia, migracionPendiente) = await PrepararAsync(idRegistro: null);
        var informeYaExistente = Informe.CrearMigrado("902/2022", new DateOnly(2022, 1, 1), dependencia.Id, Guid.NewGuid());
        dbContext.Informes.Add(informeYaExistente);
        await dbContext.SaveChangesAsync();

        var handler = CrearHandler(dbContext);
        var command = new CrearInformeDesdeMigracionPendienteCommand(migracionPendiente.Id, new DateOnly(2022, 6, 15), "902/2022");

        await Assert.ThrowsAsync<EntidadDuplicadaException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CompletarMigracionPendiente_ElIdRegistroIngresadoYaExiste_LaMigracionPendienteSigueListadaParaCorregir()
    {
        // El rechazo debe pasar ANTES de tocar
        // dbContext.MigracionesPendientes.Remove(...) — no en medio de una
        // transacción que ya empezó a mutar el estado (diseño explícito del
        // escenario Gherkin "El ID Registro ingresado ya existe").
        var (dbContext, dependencia, migracionPendiente) = await PrepararAsync(idRegistro: null);
        var informeYaExistente = Informe.CrearMigrado("903/2022", new DateOnly(2022, 1, 1), dependencia.Id, Guid.NewGuid());
        dbContext.Informes.Add(informeYaExistente);
        await dbContext.SaveChangesAsync();

        var handler = CrearHandler(dbContext);
        var command = new CrearInformeDesdeMigracionPendienteCommand(migracionPendiente.Id, new DateOnly(2022, 6, 15), "903/2022");

        await Assert.ThrowsAsync<EntidadDuplicadaException>(() => handler.Handle(command, CancellationToken.None));

        var migracionesPendientesSinTocar = dbContext.MigracionesPendientes.ToList();
        Assert.Single(migracionesPendientesSinTocar);
        Assert.Equal(migracionPendiente.Id, migracionesPendientesSinTocar[0].Id);
        Assert.Null(migracionesPendientesSinTocar[0].IdRegistro);
    }

    [Fact]
    public async Task CompletarMigracionPendiente_ConIdRegistroYaPresenteEnLaEntidadYCommandConIdRegistroDistinto_NoSePisaElOriginal()
    {
        // Diseño: "si la MigracionPendiente.IdRegistro ya estaba seteado
        // (no null), el Command puede venir con IdRegistro = null (no se
        // pisa el original) o con el mismo valor" — el caso típico es venir
        // con null (solo faltaba la fecha), pero acá se confirma
        // explícitamente que el valor de la entidad manda.
        var (dbContext, _, migracionPendiente) = await PrepararAsync(idRegistro: "700/2022");
        var handler = CrearHandler(dbContext);

        var command = new CrearInformeDesdeMigracionPendienteCommand(migracionPendiente.Id, new DateOnly(2022, 6, 15), null);

        var informeId = await handler.Handle(command, CancellationToken.None);

        var informe = await dbContext.Informes.FindAsync(informeId);
        Assert.NotNull(informe);
        Assert.Equal("700/2022", informe.IdRegistro);
    }
}
