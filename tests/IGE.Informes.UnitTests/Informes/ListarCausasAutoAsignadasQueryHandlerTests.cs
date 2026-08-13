using IGE.Informes.Application.Common.Security;
using IGE.Informes.Application.Informes.Queries.ListarCausasAutoAsignadas;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Informes;

/// <summary>
/// Alimenta /informes/causas-auto-asignadas (Admin) — reporte de Informes
/// migrados cuya Causa se vinculó automáticamente por matching de N° de
/// Pieza Sumarial (ver CausaMatcher, MigrarInformesCommandHandler y
/// CrearInformeDesdeMigracionPendienteCommandHandler), sin revisión humana
/// en el momento. Hallazgo del security-reviewer: sin este reporte, esas
/// vinculaciones quedan sin forma de auditarlas después de una migración
/// masiva.
/// </summary>
public class ListarCausasAutoAsignadasQueryHandlerTests
{
    private static readonly Guid UsuarioId = Guid.NewGuid();

    // HU-04 · Migración histórica de informes desde Drive
    // (docs/epic-01-gestion-informes.md), extensión (2026-08-12): se abre a
    // los 3 roles (Analista, Supervisor, Admin) — ya no es exclusiva de
    // Admin. Reemplaza al viejo
    // "ListarCausasAutoAsignadasQuery_DeclaraAutorizacionSoloParaAdmin" y
    // debe fallar en rojo hasta que se actualice el atributo [Autorizar].
    [Fact]
    public void ListarCausasAutoAsignadasQuery_DeclaraAutorizacion_ParaAnalistaSupervisorYAdmin()
    {
        var atributo = typeof(ListarCausasAutoAsignadasQuery)
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
    public async Task Handle_ConInformesConAuditLogDeCausaAutoAsignada_LosLista()
    {
        var dbContext = new TestAppDbContext();
        var dependencia = new Dependencia("Comisaría 2°", TipoDependencia.Comisaria);
        var causa = new Causa("AV. INFRACCION LEY 23.737", "7070029/26", "Primera Circunscripción");
        dbContext.Dependencias.Add(dependencia);
        dbContext.Causas.Add(causa);
        await dbContext.SaveChangesAsync();

        var informe = Informe.CrearMigrado("103/2020", new DateOnly(2020, 3, 10), dependencia.Id, UsuarioId, causa.Id);
        dbContext.Informes.Add(informe);
        dbContext.AuditLogs.Add(new AuditLog(UsuarioId, "CausaAutoAsignadaMigracion", nameof(Informe), informe.Id, DateTime.UtcNow));
        await dbContext.SaveChangesAsync();

        var handler = new ListarCausasAutoAsignadasQueryHandler(dbContext, new FakeAuditLogger());

        var resultado = await handler.Handle(new ListarCausasAutoAsignadasQuery(), CancellationToken.None);

        var item = Assert.Single(resultado);
        Assert.Equal(informe.Id, item.InformeId);
        Assert.Equal("103/2020", item.IdRegistro);
        Assert.Equal("AV. INFRACCION LEY 23.737", item.CausaCaratula);
        Assert.Equal("7070029/26", item.CausaNroPiezaSumarial);
    }

    [Fact]
    public async Task Handle_SinAuditLogsDeCausaAutoAsignada_DevuelveListaVacia()
    {
        var dbContext = new TestAppDbContext();
        var dependencia = new Dependencia("Comisaría 2°", TipoDependencia.Comisaria);
        dbContext.Dependencias.Add(dependencia);
        await dbContext.SaveChangesAsync();

        var informe = Informe.CrearMigrado("104/2020", new DateOnly(2020, 3, 10), dependencia.Id, UsuarioId);
        dbContext.Informes.Add(informe);
        // Otro tipo de AuditLog sobre el mismo Informe, no el que interesa.
        dbContext.AuditLogs.Add(new AuditLog(UsuarioId, "Lectura", nameof(Informe), informe.Id, DateTime.UtcNow));
        await dbContext.SaveChangesAsync();

        var handler = new ListarCausasAutoAsignadasQueryHandler(dbContext, new FakeAuditLogger());

        var resultado = await handler.Handle(new ListarCausasAutoAsignadasQuery(), CancellationToken.None);

        Assert.Empty(resultado);
    }
}
