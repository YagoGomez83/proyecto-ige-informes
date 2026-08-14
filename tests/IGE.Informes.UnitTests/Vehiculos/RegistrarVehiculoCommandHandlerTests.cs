using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Vehiculos.Commands.RegistrarVehiculo;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Vehiculos;

public class RegistrarVehiculoCommandHandlerTests
{
    [Fact]
    public async Task Registra_el_vehiculo_en_estado_Vigente_con_sus_categorias_de_alerta()
    {
        var dbContext = new TestAppDbContext();
        var robado = new CategoriaAlerta("Robado");
        var narcotrafico = new CategoriaAlerta("Narcotráfico");
        dbContext.CategoriasAlerta.Add(robado);
        dbContext.CategoriasAlerta.Add(narcotrafico);
        await dbContext.SaveChangesAsync();

        var handler = new RegistrarVehiculoCommandHandler(dbContext);

        var vehiculoId = await handler.Handle(
            new RegistrarVehiculoCommand(
                "Volkswagen", "Gol", "Gris", CertezaDominio.Incierto, AccionARealizar.Detener, "Comisaría 2°",
                TipoVehiculo.Auto, "IAK 796", null, null, [robado.Id, narcotrafico.Id]),
            CancellationToken.None);

        var vehiculo = await dbContext.Vehiculos.FindAsync(vehiculoId);
        Assert.NotNull(vehiculo);
        Assert.Equal(EstadoVehiculo.Vigente, vehiculo.Estado);
        Assert.Equal(2, vehiculo.CategoriasAlertaIds.Count);
    }

    [Fact]
    public async Task Rechaza_una_categoria_de_alerta_inexistente()
    {
        var dbContext = new TestAppDbContext();
        var handler = new RegistrarVehiculoCommandHandler(dbContext);

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            new RegistrarVehiculoCommand(
                "Volkswagen", "Gol", "Gris", CertezaDominio.Incierto, AccionARealizar.Detener, "Comisaría 2°",
                TipoVehiculo.Auto, null, null, null, [Guid.NewGuid()]),
            CancellationToken.None));
    }

    [Fact]
    public async Task Acepta_el_alta_sin_categorias_de_alerta()
    {
        var dbContext = new TestAppDbContext();
        var handler = new RegistrarVehiculoCommandHandler(dbContext);

        var vehiculoId = await handler.Handle(
            new RegistrarVehiculoCommand(
                "Volkswagen", "Gol", "Gris", CertezaDominio.Incierto, AccionARealizar.Detener, "Comisaría 2°",
                TipoVehiculo.Auto, null, null, null, []),
            CancellationToken.None);

        var vehiculo = await dbContext.Vehiculos.FindAsync(vehiculoId);
        Assert.Empty(vehiculo!.CategoriasAlertaIds);
    }

    [Fact]
    public async Task RegistrarVehiculo_ConDominioYaExistente_DebeRechazarAlta()
    {
        var dbContext = new TestAppDbContext();
        dbContext.Vehiculos.Add(new Vehiculo(
            "Volkswagen", "Gol", "Gris", CertezaDominio.Confirmado, AccionARealizar.Detener, "Comisaría 2°",
            TipoVehiculo.Auto, "IAK796", null));
        await dbContext.SaveChangesAsync();

        var handler = new RegistrarVehiculoCommandHandler(dbContext);

        await Assert.ThrowsAsync<EntidadDuplicadaException>(() => handler.Handle(
            new RegistrarVehiculoCommand(
                "Ford", "Fiesta", "Rojo", CertezaDominio.Confirmado, AccionARealizar.Identificar, "Comisaría 3°",
                TipoVehiculo.Auto, "IAK796", null, null, []),
            CancellationToken.None));
    }

    [Fact]
    public async Task RegistrarVehiculo_VariosSinDominioIdentificado_DebePermitirElAlta()
    {
        var dbContext = new TestAppDbContext();
        dbContext.Vehiculos.Add(new Vehiculo(
            "Volkswagen", "Gol", "Gris", CertezaDominio.Incierto, AccionARealizar.Detener, "Comisaría 2°",
            TipoVehiculo.Auto, null, "Vehículo sin dominio identificado"));
        await dbContext.SaveChangesAsync();

        var handler = new RegistrarVehiculoCommandHandler(dbContext);

        var vehiculoId = await handler.Handle(
            new RegistrarVehiculoCommand(
                "Ford", "Fiesta", "Rojo", CertezaDominio.Incierto, AccionARealizar.Identificar, "Comisaría 3°",
                TipoVehiculo.Auto, null, "Otro vehículo sin dominio identificado", null, []),
            CancellationToken.None);

        var vehiculo = await dbContext.Vehiculos.FindAsync(vehiculoId);
        Assert.NotNull(vehiculo);
        Assert.Null(vehiculo.Dominio);
    }

    [Fact]
    public async Task RegistrarVehiculo_ConAccionARealizarSinAccion_DebePermitirElAlta()
    {
        var dbContext = new TestAppDbContext();
        var handler = new RegistrarVehiculoCommandHandler(dbContext);

        var vehiculoId = await handler.Handle(
            new RegistrarVehiculoCommand(
                "Renault", "Clio", "Blanco", CertezaDominio.Confirmado, AccionARealizar.SinAccion, "Comisaría 4°",
                TipoVehiculo.Auto, "ABC123", "Vehículo de referencia, ya identificado en Caso anterior", null, []),
            CancellationToken.None);

        var vehiculo = await dbContext.Vehiculos.FindAsync(vehiculoId);
        Assert.NotNull(vehiculo);
        Assert.Equal(AccionARealizar.SinAccion, vehiculo.AccionARealizar);
        Assert.Equal(EstadoVehiculo.Vigente, vehiculo.Estado);
    }

    [Fact]
    public async Task RegistrarVehiculo_TipoMotoSinCilindrada_DebeRechazarElAlta()
    {
        var dbContext = new TestAppDbContext();
        var handler = new RegistrarVehiculoCommandHandler(dbContext);

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(
            new RegistrarVehiculoCommand(
                "Honda", "Wave", "Roja", CertezaDominio.Incierto, AccionARealizar.Detener, "Comisaría 2°",
                TipoVehiculo.Moto, null, null, null, []),
            CancellationToken.None));
    }
}
