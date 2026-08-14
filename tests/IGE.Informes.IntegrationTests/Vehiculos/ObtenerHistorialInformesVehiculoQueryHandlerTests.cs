using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Application.Vehiculos.Queries.ObtenerHistorialInformesVehiculo;
using IGE.Informes.Domain.Entities;
using IGE.Informes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace IGE.Informes.IntegrationTests.Vehiculos;

/// <summary>
/// HU-07 · Ficha 360° de un vehículo (Épica 02), contra Postgres real. A
/// diferencia de BuscarInformesQueryHandler (HU-05), acá el filtro sobre la
/// primitive collection Evidencia.VehiculoIds es un Contains con un único
/// Guid FIJO (el vehículo de la ficha), no con una lista externa — se
/// confirma explícitamente en este test si esa forma más simple
/// (`e.VehiculoIds.Contains(vehiculoId)`) traduce a SQL contra Npgsql, a
/// diferencia del caso de HU-05 que sí requirió resolver en memoria.
/// </summary>
public class ObtenerHistorialInformesVehiculoQueryHandlerTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private sealed class NullAuditLogger : IAuditLogger
    {
        public Task RegistrarAccesoAsync(string accion, string entidad, Guid? entidadId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact]
    public async Task ObtenerHistorial_FiltroPorVehiculoId_TraduceCorrectamenteContraPostgresReal()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using (var migrationContext = new AppDbContext(options))
        {
            await migrationContext.Database.MigrateAsync();
        }

        Guid vehiculoBuscadoId;
        Guid informeConVehiculoId;

        await using (var setupContext = new AppDbContext(options))
        {
            var dependencia = new Dependencia("Comisaría 2°", TipoDependencia.Comisaria);
            setupContext.Dependencias.Add(dependencia);

            var vehiculoBuscado = new Vehiculo(
                "Ford", "Fiesta", "Gris", CertezaDominio.Confirmado, AccionARealizar.Identificar, "Comisaría 2°", TipoVehiculo.Auto, dominio: "MRK064");
            var otroVehiculo = new Vehiculo(
                "Renault", "Clio", "Azul", CertezaDominio.Confirmado, AccionARealizar.Identificar, "Comisaría 2°", TipoVehiculo.Auto, dominio: "ABC123");
            setupContext.Vehiculos.Add(vehiculoBuscado);
            setupContext.Vehiculos.Add(otroVehiculo);
            await setupContext.SaveChangesAsync();

            var informeConVehiculo = Informe.CrearMigrado("1/2026", new DateOnly(2026, 5, 10), dependencia.Id, Guid.NewGuid());
            var informeSinVehiculo = Informe.CrearMigrado("2/2026", new DateOnly(2026, 5, 11), dependencia.Id, Guid.NewGuid());
            setupContext.Informes.Add(informeConVehiculo);
            setupContext.Informes.Add(informeSinVehiculo);

            var evidenciaConVehiculoBuscado = new Evidencia(1, informeConVehiculo.Id);
            evidenciaConVehiculoBuscado.VincularVehiculo(vehiculoBuscado.Id);
            setupContext.Evidencias.Add(evidenciaConVehiculoBuscado);

            var evidenciaConOtroVehiculo = new Evidencia(1, informeSinVehiculo.Id);
            evidenciaConOtroVehiculo.VincularVehiculo(otroVehiculo.Id);
            setupContext.Evidencias.Add(evidenciaConOtroVehiculo);
            await setupContext.SaveChangesAsync();

            vehiculoBuscadoId = vehiculoBuscado.Id;
            informeConVehiculoId = informeConVehiculo.Id;
        }

        await using var dbContext = new AppDbContext(options);
        var handler = new ObtenerHistorialInformesVehiculoQueryHandler(dbContext, new NullAuditLogger());

        var resultado = await handler.Handle(
            new ObtenerHistorialInformesVehiculoQuery(vehiculoBuscadoId), CancellationToken.None);

        Assert.Single(resultado);
        Assert.Equal(informeConVehiculoId, resultado.Single().InformeId);
    }

    [Fact]
    public async Task ObtenerHistorial_MultiplesInformesConMismoVehiculo_DevuelveOrdenadosPorFechaDescendenteContraPostgresReal()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using (var migrationContext = new AppDbContext(options))
        {
            await migrationContext.Database.MigrateAsync();
        }

        Guid vehiculoId;
        Guid informeRecienteId;
        Guid informeAntiguoId;

        await using (var setupContext = new AppDbContext(options))
        {
            var dependencia = new Dependencia("Comisaría 2°", TipoDependencia.Comisaria);
            setupContext.Dependencias.Add(dependencia);

            var vehiculo = new Vehiculo(
                "Ford", "Fiesta", "Gris", CertezaDominio.Confirmado, AccionARealizar.Identificar, "Comisaría 2°", TipoVehiculo.Auto, dominio: "MRK064");
            setupContext.Vehiculos.Add(vehiculo);
            await setupContext.SaveChangesAsync();

            var informeAntiguo = Informe.CrearMigrado("1/2026", new DateOnly(2026, 1, 10), dependencia.Id, Guid.NewGuid());
            var informeReciente = Informe.CrearMigrado("2/2026", new DateOnly(2026, 6, 15), dependencia.Id, Guid.NewGuid());
            setupContext.Informes.Add(informeAntiguo);
            setupContext.Informes.Add(informeReciente);

            var evidenciaAntigua = new Evidencia(1, informeAntiguo.Id);
            evidenciaAntigua.VincularVehiculo(vehiculo.Id);
            setupContext.Evidencias.Add(evidenciaAntigua);

            var evidenciaReciente = new Evidencia(1, informeReciente.Id);
            evidenciaReciente.VincularVehiculo(vehiculo.Id);
            setupContext.Evidencias.Add(evidenciaReciente);
            await setupContext.SaveChangesAsync();

            vehiculoId = vehiculo.Id;
            informeRecienteId = informeReciente.Id;
            informeAntiguoId = informeAntiguo.Id;
        }

        await using var dbContext = new AppDbContext(options);
        var handler = new ObtenerHistorialInformesVehiculoQueryHandler(dbContext, new NullAuditLogger());

        var resultado = (await handler.Handle(
            new ObtenerHistorialInformesVehiculoQuery(vehiculoId), CancellationToken.None)).ToList();

        Assert.Equal(2, resultado.Count);
        Assert.Equal(informeRecienteId, resultado[0].InformeId);
        Assert.Equal(informeAntiguoId, resultado[1].InformeId);
    }
}
