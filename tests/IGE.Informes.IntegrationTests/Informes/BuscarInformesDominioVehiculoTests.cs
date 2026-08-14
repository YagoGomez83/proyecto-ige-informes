using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Application.Informes.Queries.BuscarInformes;
using IGE.Informes.Domain.Entities;
using IGE.Informes.Infrastructure.Busqueda;
using IGE.Informes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace IGE.Informes.IntegrationTests.Informes;

/// <summary>
/// HU-05 · Buscar informes por múltiples criterios (Épica 02), filtro
/// DominioVehiculo. Se prueba acá contra Postgres real porque
/// EF Core InMemory soporta ToUpperInvariant() client-side, pero Npgsql no
/// tiene traducción a SQL para ese método (solo para ToUpper()) — bug real
/// encontrado en verificación manual: los tests unitarios contra InMemory
/// pasaban igual con ToUpperInvariant(), la traducción a SQL solo fallaba
/// en tiempo de ejecución contra Postgres real.
/// </summary>
public class BuscarInformesDominioVehiculoTests : IAsyncLifetime
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
    public async Task BuscarInformes_FiltroPorDominioVehiculo_TraduceCorrectamenteContraPostgresReal()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using (var migrationContext = new AppDbContext(options))
        {
            await migrationContext.Database.MigrateAsync();
        }

        Guid informeConVehiculoId;

        await using (var setupContext = new AppDbContext(options))
        {
            var dependencia = new Dependencia("Comisaría 2°", TipoDependencia.Comisaria);
            setupContext.Dependencias.Add(dependencia);

            var vehiculo = new Vehiculo(
                "Ford", "Fiesta", "Gris", CertezaDominio.Confirmado, AccionARealizar.Identificar, "Comisaría 2°", TipoVehiculo.Auto, dominio: "IAK 796");
            setupContext.Vehiculos.Add(vehiculo);
            await setupContext.SaveChangesAsync();

            var informe = Informe.CrearMigrado("1/2026", new DateOnly(2026, 5, 10), dependencia.Id, Guid.NewGuid());
            setupContext.Informes.Add(informe);

            var evidencia = new Evidencia(1, informe.Id);
            evidencia.VincularVehiculo(vehiculo.Id);
            setupContext.Evidencias.Add(evidencia);
            await setupContext.SaveChangesAsync();

            informeConVehiculoId = informe.Id;
        }

        await using var dbContext = new AppDbContext(options);
        var handler = new BuscarInformesQueryHandler(dbContext, new NullAuditLogger());

        var resultado = await handler.Handle(new BuscarInformesQuery(DominioVehiculo: "iak796"), CancellationToken.None);

        Assert.Contains(resultado, r => r.Id == informeConVehiculoId);
    }
}
