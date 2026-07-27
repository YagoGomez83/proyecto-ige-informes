using IGE.Informes.Application.Camaras.Commands.RegistrarCamara;
using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Domain.Entities;
using IGE.Informes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace IGE.Informes.IntegrationTests.Camaras;

public class RegistrarCamaraConcurrenciaTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact]
    public async Task Codigo_repetido_bajo_concurrencia_se_acepta_contra_la_base_real()
    {
        // Camara.Codigo dejó de ser único (ver docs/01-glosario-dominio.md):
        // el relevamiento real trae códigos repetidos entre cámaras de una
        // misma instalación agrupada (ej. "PLI" con 22 cámaras). Este test
        // reemplaza al anterior (que esperaba EntidadDuplicadaException bajo
        // condición de carrera) y confirma contra Postgres real que el
        // índice de Codigo ya no es UNIQUE — dos altas concurrentes con el
        // mismo Codigo deben persistir ambas sin conflicto.
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using (var migrationContext = new AppDbContext(options))
        {
            await migrationContext.Database.MigrateAsync();
        }

        await using (var altaConcurrente = new AppDbContext(options))
        {
            altaConcurrente.Camaras.Add(new Camara("PLI", TipoCamara.Lpr, "La Punilla - Ingreso"));
            await altaConcurrente.SaveChangesAsync();
        }

        await using var dbContext = new AppDbContext(options);
        var handler = new RegistrarCamaraCommandHandler(dbContext);

        var camaraId = await handler.Handle(
            new RegistrarCamaraCommand("PLI", TipoCamara.Lpr, "La Punilla - Egreso"), CancellationToken.None);

        var camara = await dbContext.Camaras.FindAsync(camaraId);
        Assert.Equal("PLI", camara!.Codigo);
    }
}
