using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Informes.Commands.PublicarInforme;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Informes;

/// <summary>
/// HU-03 · Publicar / firmar un informe (Épica 01).
///
/// Alcance de esta primera pasada (confirmado): un solo
/// PublicarInformeCommand(InformeId). Si el usuario actual todavía no
/// figura como Firmante del Informe, el Handler lo agrega automáticamente
/// (Informe.AgregarFirmante) y publica en la misma operación
/// (Informe.Publicar()) — un solo click publica y firma a la vez. No hay
/// Command separado para gestionar Firmantes en esta HU.
///
/// Como el Handler siempre llama AgregarFirmante antes de Publicar(), el
/// único de los 3 rechazos de Publicar() que se puede disparar en la
/// práctica desde acá es el de Causa/Dependencia faltante — el de "falta
/// Firmante" ya está cubierto a nivel de dominio en
/// tests/IGE.Informes.UnitTests/Domain/InformeTests.cs
/// (Publicar_sin_Firmante_lo_rechaza) y no se duplica acá.
///
/// Los escenarios que agregan un InformeAnalista (owned collection, Add
/// posterior a un SaveChanges previo del mismo Informe) NO se testean acá:
/// EF Core InMemory tiene una limitación conocida con OwnsMany + Add sobre
/// una entidad padre ya persistida (DbUpdateConcurrencyException espuria,
/// no ocurre contra Postgres real) — ver
/// tests/IGE.Informes.IntegrationTests/Informes/PublicarInformeAuditLogTests.cs,
/// que cubre publicación exitosa + agregado de firmante + no-op sobre
/// Informe ya Publicado, contra Postgres real vía Testcontainers. Mismo
/// criterio que ya documenta la memoria del proyecto: InMemory no alcanza
/// para todo, Testcontainers es la fuente de verdad cuando InMemory no es
/// fiel al comportamiento real.
///
/// La auditoría de la publicación (AuditLog con usuario y fecha/hora)
/// también se verifica ahí, mismo patrón que EditarInformeAuditLogTests.
/// </summary>
public class PublicarInformeCommandHandlerTests
{
    private static readonly Guid AnalistaSolicitanteId = Guid.NewGuid();

    private static async Task<(TestAppDbContext DbContext, Informe Informe)> PrepararAsync(Guid? causaId)
    {
        var dbContext = new TestAppDbContext();
        var caso = new CasoAnalisis(new DateOnly(2026, 7, 21), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var dependencia = new Dependencia("Comisaría 2°", TipoDependencia.Comisaria);

        dbContext.CasosAnalisis.Add(caso);
        dbContext.Dependencias.Add(dependencia);

        Guid? causaIdReal = null;
        if (causaId is not null)
        {
            var causa = new Causa("N.N. s/Robo", "7070029/26", "Primera Circunscripción");
            dbContext.Causas.Add(causa);
            causaIdReal = causa.Id;
        }

        await dbContext.SaveChangesAsync();

        var informe = new Informe(
            "290/2026",
            new DateOnly(2026, 7, 14),
            caso.Id,
            dependencia.Id,
            AnalistaSolicitanteId,
            causaIdReal);
        informe.CompletarRelato("Relato original extraído del PDF.");

        dbContext.Informes.Add(informe);
        await dbContext.SaveChangesAsync();

        return (dbContext, informe);
    }

    [Fact]
    public async Task PublicarInforme_SinCausaAsociada_RechazaConInvalidOperationException()
    {
        var (dbContext, informe) = await PrepararAsync(causaId: null);
        var usuarioActualId = Guid.NewGuid();

        var handler = new PublicarInformeCommandHandler(dbContext, new FakeCurrentUserService(usuarioActualId));
        var command = new PublicarInformeCommand(informe.Id);

        var excepcion = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(command, CancellationToken.None));

        Assert.Equal("No se puede publicar el Informe: falta la Causa.", excepcion.Message);

        var informeSinCambios = await dbContext.Informes.FindAsync(informe.Id);
        Assert.NotNull(informeSinCambios);
        Assert.Equal(EstadoInforme.Borrador, informeSinCambios.Estado);
    }

    [Fact]
    public async Task PublicarInforme_InformeInexistente_RechazaConEntidadNoEncontradaException()
    {
        var dbContext = new TestAppDbContext();
        var handler = new PublicarInformeCommandHandler(dbContext, new FakeCurrentUserService(Guid.NewGuid()));

        var idInexistente = Guid.NewGuid();
        var command = new PublicarInformeCommand(idInexistente);

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task PublicarInforme_SinUsuarioAutenticado_RechazaConForbiddenAccessException()
    {
        var (dbContext, informe) = await PrepararAsync(causaId: Guid.NewGuid());
        var handler = new PublicarInformeCommandHandler(dbContext, new FakeCurrentUserService(usuarioId: null));

        var command = new PublicarInformeCommand(informe.Id);

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => handler.Handle(command, CancellationToken.None));
    }

}
