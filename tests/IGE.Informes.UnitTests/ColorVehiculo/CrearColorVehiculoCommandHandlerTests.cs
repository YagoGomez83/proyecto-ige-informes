using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.ColorVehiculo.Commands.CrearColorVehiculo;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.ColorVehiculo;

/// <summary>
/// HU-20 (Catálogo de Marca y Color de Vehículo) — cubre el Handler de
/// CrearColorVehiculoCommand, mismo patrón que CrearTipoCausaCommandHandlerTests
/// / CrearMarcaVehiculoCommandHandlerTests (chequeo de duplicado en memoria
/// antes del SaveChanges). El constraint único a nivel de DB real se cubre
/// aparte con Testcontainers, ya que EF Core InMemory no lo aplica.
///
/// Escenario Gherkin "Alta de un Color" -> Registra_un_color_nuevo.
/// Escenario Gherkin "Nombre duplicado" -> Rechaza_un_nombre_duplicado.
/// </summary>
public class CrearColorVehiculoCommandHandlerTests
{
    [Fact]
    public async Task CrearColorVehiculo_NombreValido_RegistraElColorYQuedaDisponibleEnElCatalogo()
    {
        var dbContext = new TestAppDbContext();
        var handler = new CrearColorVehiculoCommandHandler(dbContext);

        var colorId = await handler.Handle(
            new CrearColorVehiculoCommand("Gris Oscuro"), CancellationToken.None);

        var color = await dbContext.ColoresVehiculo.FindAsync(colorId);
        Assert.NotNull(color);
        Assert.Equal("Gris Oscuro", color!.Nombre);
    }

    [Fact]
    public async Task CrearColorVehiculo_NombreDuplicado_RechazaElAltaConEntidadDuplicadaException()
    {
        var dbContext = new TestAppDbContext();
        dbContext.ColoresVehiculo.Add(new IGE.Informes.Domain.Entities.ColorVehiculo("Blanco"));
        await dbContext.SaveChangesAsync();

        var handler = new CrearColorVehiculoCommandHandler(dbContext);

        var excepcion = await Assert.ThrowsAsync<EntidadDuplicadaException>(() => handler.Handle(
            new CrearColorVehiculoCommand("Blanco"), CancellationToken.None));

        Assert.Contains("Blanco", excepcion.Message);
    }

    [Fact]
    public async Task CrearColorVehiculo_NombreDuplicado_NoModificaElCatalogoExistente()
    {
        var dbContext = new TestAppDbContext();
        var original = new IGE.Informes.Domain.Entities.ColorVehiculo("Blanco");
        dbContext.ColoresVehiculo.Add(original);
        await dbContext.SaveChangesAsync();

        var handler = new CrearColorVehiculoCommandHandler(dbContext);

        await Assert.ThrowsAsync<EntidadDuplicadaException>(() => handler.Handle(
            new CrearColorVehiculoCommand("Blanco"), CancellationToken.None));

        Assert.Single(dbContext.ColoresVehiculo);
        var colorSinTocar = await dbContext.ColoresVehiculo.FindAsync(original.Id);
        Assert.Equal("Blanco", colorSinTocar!.Nombre);
    }
}
