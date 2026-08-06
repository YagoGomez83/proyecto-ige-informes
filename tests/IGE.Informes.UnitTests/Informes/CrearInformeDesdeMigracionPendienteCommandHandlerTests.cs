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
/// escenario "Completar la Fecha de Análisis de una Migración Pendiente".
/// El Command/Handler todavía no existen: estos tests están escritos antes
/// de la implementación (TDD), por lo que deben fallar en rojo hasta que
/// se cree
/// IGE.Informes.Application.Informes.Commands.CrearInformeDesdeMigracionPendiente.*
/// — pantalla Admin /informes/migrar/pendientes.
/// </summary>
public class CrearInformeDesdeMigracionPendienteCommandHandlerTests
{
    private static readonly Guid UsuarioMigradorOriginalId = Guid.NewGuid();
    private static readonly Guid UsuarioAdminQueCompletaId = Guid.NewGuid();

    private static async Task<(TestAppDbContext DbContext, Dependencia Dependencia, MigracionPendiente MigracionPendiente)> PrepararAsync(
        string idRegistro = "700/2022",
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

        var command = new CrearInformeDesdeMigracionPendienteCommand(migracionPendiente.Id, fechaIngresada);

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

        var command = new CrearInformeDesdeMigracionPendienteCommand(migracionPendiente.Id, new DateOnly(2022, 6, 15));

        await handler.Handle(command, CancellationToken.None);

        Assert.Empty(dbContext.MigracionesPendientes.ToList());
    }

    [Fact]
    public async Task CompletarMigracionPendiente_MigracionPendienteInexistente_RechazaConEntidadNoEncontrada()
    {
        var dbContext = new TestAppDbContext();
        var handler = CrearHandler(dbContext);

        var command = new CrearInformeDesdeMigracionPendienteCommand(Guid.NewGuid(), new DateOnly(2022, 6, 15));

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
        var command = new CrearInformeDesdeMigracionPendienteCommand(migracionPendiente.Id, new DateOnly(2022, 6, 15));

        await Assert.ThrowsAsync<EntidadDuplicadaException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task CompletarMigracionPendiente_SinCausaExtraidaPorElParser_CreaElInformeSinCausaAsociada()
    {
        var (dbContext, _, migracionPendiente) = await PrepararAsync(causaCaratula: null, piezaSumarial: null);
        var handler = CrearHandler(dbContext);

        var command = new CrearInformeDesdeMigracionPendienteCommand(migracionPendiente.Id, new DateOnly(2022, 6, 15));

        var informeId = await handler.Handle(command, CancellationToken.None);

        var informe = await dbContext.Informes.FindAsync(informeId);
        Assert.NotNull(informe);
        Assert.Null(informe.CausaId);
    }

    [Fact]
    public void CrearInformeDesdeMigracionPendienteCommand_DeclaraAutorizacion_SoloParaRolAdmin()
    {
        var atributo = typeof(CrearInformeDesdeMigracionPendienteCommand)
            .GetCustomAttributes(typeof(AutorizarAttribute), inherit: true)
            .Cast<AutorizarAttribute>()
            .SingleOrDefault();

        Assert.NotNull(atributo);
        Assert.Single(atributo.Roles);
        Assert.Equal(Roles.Admin, atributo.Roles.Single());
    }

    [Fact]
    public async Task CompletarMigracionPendiente_RegistraLaCreacionDelInformeEnAuditLog()
    {
        var (dbContext, _, migracionPendiente) = await PrepararAsync();
        var auditLogger = new FakeAuditLogger();
        var handler = CrearHandler(dbContext, auditLogger);

        var command = new CrearInformeDesdeMigracionPendienteCommand(migracionPendiente.Id, new DateOnly(2022, 6, 15));

        await handler.Handle(command, CancellationToken.None);

        Assert.Contains(auditLogger.Registros, r => r.Entidad == nameof(Informe));
    }
}
