using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Application.Vehiculos.Queries.ListarVehiculos;
using IGE.Informes.Domain.Entities;
using IGE.Informes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace IGE.Informes.IntegrationTests.Vehiculos;

/// <summary>
/// El orden usa una expresión condicional sobre el enum Estado
/// (v.Estado == EstadoVehiculo.Vigente ? 0 : 1) — no traduce igual en EF
/// Core InMemory que en Npgsql real, mismo motivo que
/// BuscarVehiculosQueryHandlerTests, por eso se prueba acá.
/// </summary>
public class ListarVehiculosOrdenTests : IAsyncLifetime
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
    public async Task ListarVehiculos_OrdenaVigentesPrimeroYAlfabeticoDentroDeCadaEstado()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using (var migrationContext = new AppDbContext(options))
        {
            await migrationContext.Database.MigrateAsync();
        }

        await using (var setupContext = new AppDbContext(options))
        {
            var zeta = new Vehiculo("Zeta", "Uno", "Gris", CertezaDominio.Confirmado, AccionARealizar.Identificar, "Comisaría");
            var alfa = new Vehiculo("Alfa", "Uno", "Gris", CertezaDominio.Confirmado, AccionARealizar.Identificar, "Comisaría");
            var beta = new Vehiculo("Beta", "Uno", "Gris", CertezaDominio.Confirmado, AccionARealizar.Identificar, "Comisaría");
            beta.MarcarIdentificado();

            // Orden de inserción deliberadamente distinto al orden esperado.
            setupContext.Vehiculos.AddRange(zeta, beta, alfa);
            await setupContext.SaveChangesAsync();
        }

        await using var dbContext = new AppDbContext(options);
        var handler = new ListarVehiculosQueryHandler(dbContext, new NullAuditLogger());

        var resultado = await handler.Handle(new ListarVehiculosQuery(Pagina: 1, TamanioPagina: 50), CancellationToken.None);

        var marcas = resultado.Items.Select(v => v.Marca).ToList();

        // Alfa y Zeta (Vigente) antes que Beta (Identificado); dentro de
        // Vigente, alfabético: Alfa antes que Zeta.
        Assert.Equal(["Alfa", "Zeta", "Beta"], marcas);
    }
}
