using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.Informes.Commands.VincularVehiculoInforme;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Informes;

public class VincularVehiculoInformeCommandHandlerTests
{
    private static Informe CrearInforme() =>
        Informe.CrearMigrado("290/2026", new DateOnly(2026, 7, 21), Guid.NewGuid(), Guid.NewGuid());

    private static Vehiculo CrearVehiculo() =>
        new("Ford", "Fiesta", "Gris", CertezaDominio.Confirmado, AccionARealizar.Identificar, "Comisaría 2°", TipoVehiculo.Auto);

    [Fact]
    public async Task Vincula_el_vehiculo_al_informe_arrancando_el_numero_de_imagen_en_1()
    {
        var dbContext = new TestAppDbContext();
        var informe = CrearInforme();
        var vehiculo = CrearVehiculo();
        dbContext.Informes.Add(informe);
        dbContext.Vehiculos.Add(vehiculo);
        await dbContext.SaveChangesAsync();

        var handler = new VincularVehiculoInformeCommandHandler(dbContext);
        await handler.Handle(new VincularVehiculoInformeCommand(informe.Id, vehiculo.Id), CancellationToken.None);

        var evidencia = Assert.Single(dbContext.Evidencias);
        Assert.Equal(1, evidencia.NumeroImagen);
        Assert.Contains(vehiculo.Id, evidencia.VehiculoIds);
    }

    [Fact]
    public async Task Autoasigna_el_siguiente_numero_de_imagen_si_ya_hay_evidencias_del_pdf()
    {
        var dbContext = new TestAppDbContext();
        var informe = CrearInforme();
        var vehiculo = CrearVehiculo();
        dbContext.Informes.Add(informe);
        dbContext.Vehiculos.Add(vehiculo);
        dbContext.Evidencias.Add(new Evidencia(1, informe.Id));
        dbContext.Evidencias.Add(new Evidencia(3, informe.Id));
        await dbContext.SaveChangesAsync();

        var handler = new VincularVehiculoInformeCommandHandler(dbContext);
        await handler.Handle(new VincularVehiculoInformeCommand(informe.Id, vehiculo.Id), CancellationToken.None);

        var nueva = dbContext.Evidencias.Single(e => e.VehiculoIds.Contains(vehiculo.Id));
        Assert.Equal(4, nueva.NumeroImagen);
    }

    [Fact]
    public async Task Vincular_el_mismo_vehiculo_dos_veces_es_idempotente()
    {
        var dbContext = new TestAppDbContext();
        var informe = CrearInforme();
        var vehiculo = CrearVehiculo();
        dbContext.Informes.Add(informe);
        dbContext.Vehiculos.Add(vehiculo);
        await dbContext.SaveChangesAsync();

        var handler = new VincularVehiculoInformeCommandHandler(dbContext);
        await handler.Handle(new VincularVehiculoInformeCommand(informe.Id, vehiculo.Id), CancellationToken.None);
        await handler.Handle(new VincularVehiculoInformeCommand(informe.Id, vehiculo.Id), CancellationToken.None);

        Assert.Single(dbContext.Evidencias);
    }

    [Fact]
    public async Task Rechaza_un_informe_inexistente()
    {
        var dbContext = new TestAppDbContext();
        var vehiculo = CrearVehiculo();
        dbContext.Vehiculos.Add(vehiculo);
        await dbContext.SaveChangesAsync();

        var handler = new VincularVehiculoInformeCommandHandler(dbContext);

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            new VincularVehiculoInformeCommand(Guid.NewGuid(), vehiculo.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Rechaza_un_vehiculo_inexistente()
    {
        var dbContext = new TestAppDbContext();
        var informe = CrearInforme();
        dbContext.Informes.Add(informe);
        await dbContext.SaveChangesAsync();

        var handler = new VincularVehiculoInformeCommandHandler(dbContext);

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => handler.Handle(
            new VincularVehiculoInformeCommand(informe.Id, Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Rechaza_vincular_a_un_informe_publicado()
    {
        var dbContext = new TestAppDbContext();
        var informe = new Informe("290/2026", new DateOnly(2026, 7, 21), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        informe.AgregarFirmante(Guid.NewGuid());
        informe.Publicar();
        var vehiculo = CrearVehiculo();
        dbContext.Informes.Add(informe);
        dbContext.Vehiculos.Add(vehiculo);
        await dbContext.SaveChangesAsync();

        var handler = new VincularVehiculoInformeCommandHandler(dbContext);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            new VincularVehiculoInformeCommand(informe.Id, vehiculo.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Vincular_un_vehiculo_ya_vinculado_a_otro_informe_genera_Alerta_de_reincidencia()
    {
        var dbContext = new TestAppDbContext();
        var informeAnterior = CrearInforme();
        var informeNuevo = CrearInforme();
        var vehiculo = CrearVehiculo();
        dbContext.Informes.Add(informeAnterior);
        dbContext.Informes.Add(informeNuevo);
        dbContext.Vehiculos.Add(vehiculo);

        var evidenciaAnterior = new Evidencia(1, informeAnterior.Id);
        evidenciaAnterior.VincularVehiculo(vehiculo.Id);
        dbContext.Evidencias.Add(evidenciaAnterior);
        await dbContext.SaveChangesAsync();

        var handler = new VincularVehiculoInformeCommandHandler(dbContext);
        await handler.Handle(new VincularVehiculoInformeCommand(informeNuevo.Id, vehiculo.Id), CancellationToken.None);

        var alerta = Assert.Single(dbContext.Alertas);
        Assert.Equal(TipoAlerta.ReincidenciaOtroInforme, alerta.Tipo);
        Assert.Equal(vehiculo.Id, alerta.VehiculoId);
        Assert.Equal(informeNuevo.Id, alerta.InformeId);
        Assert.Equal(informeAnterior.Id, alerta.InformePrevioId);
    }

    [Fact]
    public async Task Vincular_un_vehiculo_sin_vinculo_previo_genera_Alerta_de_carga_huerfana()
    {
        var dbContext = new TestAppDbContext();
        var informe = CrearInforme();
        var vehiculo = CrearVehiculo();
        dbContext.Informes.Add(informe);
        dbContext.Vehiculos.Add(vehiculo);
        await dbContext.SaveChangesAsync();

        var handler = new VincularVehiculoInformeCommandHandler(dbContext);
        await handler.Handle(new VincularVehiculoInformeCommand(informe.Id, vehiculo.Id), CancellationToken.None);

        var alerta = Assert.Single(dbContext.Alertas);
        Assert.Equal(TipoAlerta.CargaHuerfana, alerta.Tipo);
        Assert.Equal(vehiculo.Id, alerta.VehiculoId);
        Assert.Equal(informe.Id, alerta.InformeId);
        Assert.Null(alerta.InformePrevioId);
    }

    [Fact]
    public async Task Vincular_el_mismo_vehiculo_dos_veces_al_mismo_informe_no_genera_Alerta_duplicada()
    {
        var dbContext = new TestAppDbContext();
        var informe = CrearInforme();
        var vehiculo = CrearVehiculo();
        dbContext.Informes.Add(informe);
        dbContext.Vehiculos.Add(vehiculo);
        await dbContext.SaveChangesAsync();

        var handler = new VincularVehiculoInformeCommandHandler(dbContext);
        await handler.Handle(new VincularVehiculoInformeCommand(informe.Id, vehiculo.Id), CancellationToken.None);
        await handler.Handle(new VincularVehiculoInformeCommand(informe.Id, vehiculo.Id), CancellationToken.None);

        // La primera vinculación genera la Alerta de carga huérfana; la
        // segunda es idempotente (Handler.Handle retorna temprano) y no
        // vuelve a evaluar el chequeo de Alerta.
        Assert.Single(dbContext.Alertas);
    }
}
