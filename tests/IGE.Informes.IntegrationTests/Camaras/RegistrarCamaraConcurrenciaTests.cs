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
    public async Task Carrera_entre_el_chequeo_y_el_insert_termina_en_EntidadDuplicadaException()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using (var migrationContext = new AppDbContext(options))
        {
            await migrationContext.Database.MigrateAsync();
        }

        await using var dbContext = new AppDbContext(options);
        var handler = new RegistrarCamaraCommandHandler(dbContext);

        // Reproduce la ventana de carrera que el AnyAsync del Handler no
        // puede ver: otra alta con el mismo Codigo se persiste, por su
        // cuenta, en el instante entre el chequeo del Handler y su propio
        // SaveChangesAsync — el índice único de la base es quien atrapa
        // el conflicto, no el AnyAsync.
        await using (var altaConcurrente = new AppDbContext(options))
        {
            altaConcurrente.Camaras.Add(new Camara("SL 18", TipoCamara.Domo));
            await altaConcurrente.SaveChangesAsync();
        }

        await Assert.ThrowsAsync<EntidadDuplicadaException>(() => handler.Handle(
            new RegistrarCamaraCommand("SL 18", TipoCamara.Lpr, null), CancellationToken.None));
    }
}
