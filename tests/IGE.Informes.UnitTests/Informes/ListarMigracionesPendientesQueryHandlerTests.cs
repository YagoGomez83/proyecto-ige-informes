using IGE.Informes.Application.Common.Security;
using IGE.Informes.Application.Informes.Queries.ListarMigracionesPendientes;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Informes;

/// <summary>
/// HU-04 · Migración histórica de informes desde Drive
/// (docs/epic-01-gestion-informes.md) — Característica "Migración masiva",
/// escenario "Completar la Fecha de Análisis de una Migración Pendiente":
/// alimenta la pantalla Admin /informes/migrar/pendientes que lista las
/// MigracionPendiente existentes. El Query/Handler todavía no existen:
/// estos tests están escritos antes de la implementación (TDD), por lo que
/// deben fallar en rojo hasta que se cree
/// IGE.Informes.Application.Informes.Queries.ListarMigracionesPendientes.*.
/// </summary>
public class ListarMigracionesPendientesQueryHandlerTests
{
    private static async Task<(TestAppDbContext DbContext, Dependencia Dependencia)> PrepararAsync()
    {
        var dbContext = new TestAppDbContext();
        var dependencia = new Dependencia("Comisaría 2°", TipoDependencia.Comisaria);
        dbContext.Dependencias.Add(dependencia);
        await dbContext.SaveChangesAsync();

        return (dbContext, dependencia);
    }

    [Fact]
    public async Task ListarMigracionesPendientes_ConMigracionesExistentes_LasDevuelveTodas()
    {
        var (dbContext, dependencia) = await PrepararAsync();
        dbContext.MigracionesPendientes.Add(new MigracionPendiente(
            "700/2022", "migraciones-pendientes/700-2022.pdf", dependencia.Id, Guid.NewGuid(), "Causa A", "111/22", "Relato A"));
        dbContext.MigracionesPendientes.Add(new MigracionPendiente(
            "701/2022", "migraciones-pendientes/701-2022.pdf", dependencia.Id, Guid.NewGuid(), null, null, "Relato B"));
        await dbContext.SaveChangesAsync();

        var handler = new ListarMigracionesPendientesQueryHandler(dbContext, new FakeAuditLogger());

        var resultado = await handler.Handle(new ListarMigracionesPendientesQuery(), CancellationToken.None);

        Assert.Equal(2, resultado.Count);
        Assert.Contains(resultado, m => m.IdRegistro == "700/2022");
        Assert.Contains(resultado, m => m.IdRegistro == "701/2022");
    }

    [Fact]
    public async Task ListarMigracionesPendientes_SinMigracionesPendientes_DevuelveListaVacia()
    {
        var dbContext = new TestAppDbContext();
        var handler = new ListarMigracionesPendientesQueryHandler(dbContext, new FakeAuditLogger());

        var resultado = await handler.Handle(new ListarMigracionesPendientesQuery(), CancellationToken.None);

        Assert.Empty(resultado);
    }

    [Fact]
    public async Task ListarMigracionesPendientes_TrasCompletarUna_YaNoLaLista()
    {
        // Cierra el círculo del escenario Gherkin: "la Migración Pendiente
        // deja de listarse" tras completarla — acá se verifica desde el
        // lado de la Query, simulando que ya fue borrada por
        // CrearInformeDesdeMigracionPendienteCommandHandler.
        var (dbContext, dependencia) = await PrepararAsync();
        var migracionPendiente = new MigracionPendiente(
            "702/2022", "migraciones-pendientes/702-2022.pdf", dependencia.Id, Guid.NewGuid());
        dbContext.MigracionesPendientes.Add(migracionPendiente);
        await dbContext.SaveChangesAsync();

        dbContext.MigracionesPendientes.Remove(migracionPendiente);
        await dbContext.SaveChangesAsync();

        var handler = new ListarMigracionesPendientesQueryHandler(dbContext, new FakeAuditLogger());

        var resultado = await handler.Handle(new ListarMigracionesPendientesQuery(), CancellationToken.None);

        Assert.Empty(resultado);
    }

    [Fact]
    public void ListarMigracionesPendientesQuery_DeclaraAutorizacion_SoloParaRolAdmin()
    {
        var atributo = typeof(ListarMigracionesPendientesQuery)
            .GetCustomAttributes(typeof(AutorizarAttribute), inherit: true)
            .Cast<AutorizarAttribute>()
            .SingleOrDefault();

        Assert.NotNull(atributo);
        Assert.Single(atributo.Roles);
        Assert.Equal(Roles.Admin, atributo.Roles.Single());
    }

    [Fact]
    public async Task ListarMigracionesPendientes_RegistraElAccesoEnAuditLog()
    {
        var (dbContext, dependencia) = await PrepararAsync();
        dbContext.MigracionesPendientes.Add(new MigracionPendiente(
            "703/2022", "migraciones-pendientes/703-2022.pdf", dependencia.Id, Guid.NewGuid()));
        await dbContext.SaveChangesAsync();

        var auditLogger = new FakeAuditLogger();
        var handler = new ListarMigracionesPendientesQueryHandler(dbContext, auditLogger);

        await handler.Handle(new ListarMigracionesPendientesQuery(), CancellationToken.None);

        Assert.Contains(auditLogger.Registros, r => r.Entidad == nameof(MigracionPendiente));
    }
}
