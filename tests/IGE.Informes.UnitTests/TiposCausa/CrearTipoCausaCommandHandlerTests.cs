using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.TipoCausa.Commands.CrearTipoCausa;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.TiposCausa;

/// <summary>
/// HU-19 (Catálogo de Tipos de Causa) — cubre el Handler de
/// CrearTipoCausaCommand, mismo patrón que CrearDependenciaCommandHandlerTests
/// / CrearTipoIncidenteCommandHandlerTests (chequeo de duplicado en memoria
/// antes del SaveChanges). El constraint único a nivel de DB real se cubre
/// aparte con Testcontainers, ya que EF Core InMemory no lo aplica.
///
/// Escenario Gherkin "Alta de un Tipo de Causa" -> Registra_un_tipo_de_causa_nuevo.
/// Escenario Gherkin "Nombre duplicado" -> Rechaza_un_nombre_duplicado.
/// Escenario Gherkin "Agregar un Tipo de Causa faltante sin salir de la
/// edición del Informe" es UI (atajo en Editar.razor que reusa este mismo
/// Command) — no requiere un test de Handler aparte, ver
/// docs/epic-04-gestion-catalogos.md HU-19.
/// </summary>
public class CrearTipoCausaCommandHandlerTests
{
    [Fact]
    public async Task CrearTipoCausa_NombreValido_RegistraElTipoDeCausaYQuedaDisponibleEnElCatalogo()
    {
        var dbContext = new TestAppDbContext();
        var handler = new CrearTipoCausaCommandHandler(dbContext);

        var tipoCausaId = await handler.Handle(
            new CrearTipoCausaCommand("AV. ROBO CALIFICADO"), CancellationToken.None);

        var tipoCausa = await dbContext.TiposCausa.FindAsync(tipoCausaId);
        Assert.NotNull(tipoCausa);
        Assert.Equal("AV. ROBO CALIFICADO", tipoCausa!.Nombre);
    }

    [Fact]
    public async Task CrearTipoCausa_NombreDuplicado_RechazaElAltaConEntidadDuplicadaException()
    {
        var dbContext = new TestAppDbContext();
        dbContext.TiposCausa.Add(new TipoCausa("AV. ROBO"));
        await dbContext.SaveChangesAsync();

        var handler = new CrearTipoCausaCommandHandler(dbContext);

        var excepcion = await Assert.ThrowsAsync<EntidadDuplicadaException>(() => handler.Handle(
            new CrearTipoCausaCommand("AV. ROBO"), CancellationToken.None));

        Assert.Contains("AV. ROBO", excepcion.Message);
    }

    [Fact]
    public async Task CrearTipoCausa_NombreDuplicado_NoModificaElCatalogoExistente()
    {
        var dbContext = new TestAppDbContext();
        var original = new TipoCausa("AV. ROBO");
        dbContext.TiposCausa.Add(original);
        await dbContext.SaveChangesAsync();

        var handler = new CrearTipoCausaCommandHandler(dbContext);

        await Assert.ThrowsAsync<EntidadDuplicadaException>(() => handler.Handle(
            new CrearTipoCausaCommand("AV. ROBO"), CancellationToken.None));

        Assert.Single(dbContext.TiposCausa);
        var tipoCausaSinTocar = await dbContext.TiposCausa.FindAsync(original.Id);
        Assert.Equal("AV. ROBO", tipoCausaSinTocar!.Nombre);
    }
}
