using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.MarcaVehiculo.Commands.CrearMarcaVehiculo;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.MarcaVehiculo;

/// <summary>
/// HU-20 (Catálogo de Marca y Color de Vehículo) — cubre el Handler de
/// CrearMarcaVehiculoCommand, mismo patrón que CrearTipoCausaCommandHandlerTests
/// (chequeo de duplicado en memoria antes del SaveChanges). El constraint
/// único a nivel de DB real se cubre aparte con Testcontainers, ya que EF
/// Core InMemory no lo aplica.
///
/// Escenario Gherkin "Alta de una Marca" -> Registra_una_marca_nueva.
/// Escenario Gherkin "Nombre duplicado" -> Rechaza_un_nombre_duplicado.
/// </summary>
public class CrearMarcaVehiculoCommandHandlerTests
{
    [Fact]
    public async Task CrearMarcaVehiculo_NombreValido_RegistraLaMarcaYQuedaDisponibleEnElCatalogo()
    {
        var dbContext = new TestAppDbContext();
        var handler = new CrearMarcaVehiculoCommandHandler(dbContext);

        var marcaId = await handler.Handle(
            new CrearMarcaVehiculoCommand("Chevrolet"), CancellationToken.None);

        var marca = await dbContext.MarcasVehiculo.FindAsync(marcaId);
        Assert.NotNull(marca);
        Assert.Equal("Chevrolet", marca!.Nombre);
    }

    [Fact]
    public async Task CrearMarcaVehiculo_NombreDuplicado_RechazaElAltaConEntidadDuplicadaException()
    {
        var dbContext = new TestAppDbContext();
        dbContext.MarcasVehiculo.Add(new IGE.Informes.Domain.Entities.MarcaVehiculo("Ford"));
        await dbContext.SaveChangesAsync();

        var handler = new CrearMarcaVehiculoCommandHandler(dbContext);

        var excepcion = await Assert.ThrowsAsync<EntidadDuplicadaException>(() => handler.Handle(
            new CrearMarcaVehiculoCommand("Ford"), CancellationToken.None));

        Assert.Contains("Ford", excepcion.Message);
    }

    [Fact]
    public async Task CrearMarcaVehiculo_NombreDuplicado_NoModificaElCatalogoExistente()
    {
        var dbContext = new TestAppDbContext();
        var original = new IGE.Informes.Domain.Entities.MarcaVehiculo("Ford");
        dbContext.MarcasVehiculo.Add(original);
        await dbContext.SaveChangesAsync();

        var handler = new CrearMarcaVehiculoCommandHandler(dbContext);

        await Assert.ThrowsAsync<EntidadDuplicadaException>(() => handler.Handle(
            new CrearMarcaVehiculoCommand("Ford"), CancellationToken.None));

        Assert.Single(dbContext.MarcasVehiculo);
        var marcaSinTocar = await dbContext.MarcasVehiculo.FindAsync(original.Id);
        Assert.Equal("Ford", marcaSinTocar!.Nombre);
    }
}
