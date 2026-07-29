using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.TiposIncidente.Commands.CrearTipoIncidente;
using IGE.Informes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace IGE.Informes.IntegrationTests.TiposIncidente;

/// <summary>
/// HU-18 (Catálogo de Tipos de Incidente) — confirma contra Postgres real
/// que el índice único IX_TiposIncidente_Codigo (configurado en
/// TipoIncidenteConfiguration) efectivamente rechaza un alta con Codigo
/// duplicado a nivel de base de datos, aun si dos requests concurrentes
/// pasan ambas el chequeo en memoria del Handler (AnyAsync) antes del
/// SaveChanges — la segunda debe fallar por DbUpdateException y traducirse
/// a EntidadDuplicadaException. Mismo patrón que
/// AsignarBarrioADependenciaAuditLogTests, sin AuditLog porque este catálogo
/// no lo requiere (ver notas de modelado de HU-18).
/// </summary>
public class CrearTipoIncidenteConstraintUnicoTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact]
    public async Task Rechaza_codigo_duplicado_por_el_indice_unico_de_la_base_de_datos()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using (var migrationContext = new AppDbContext(options))
        {
            await migrationContext.Database.MigrateAsync();
        }

        await using (var primerContext = new AppDbContext(options))
        {
            var primerHandler = new CrearTipoIncidenteCommandHandler(primerContext);
            await primerHandler.Handle(new CrearTipoIncidenteCommand("25", "Persona sospechosa"), CancellationToken.None);
        }

        // Un segundo DbContext que no ve el registro recién insertado en su
        // propio ChangeTracker simula la condición de carrera: el chequeo en
        // memoria (AnyAsync) del Handler no alcanza a evitarlo por sí solo,
        // así que la protección real depende del índice único de Postgres.
        await using var segundoContext = new AppDbContext(options);
        var segundoHandler = new CrearTipoIncidenteCommandHandler(segundoContext);

        await Assert.ThrowsAsync<EntidadDuplicadaException>(() => segundoHandler.Handle(
            new CrearTipoIncidenteCommand("25", "Asalto a mano armada"), CancellationToken.None));
    }

    [Fact]
    public async Task Registra_el_tipo_de_incidente_y_queda_disponible_para_el_listado()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using (var migrationContext = new AppDbContext(options))
        {
            await migrationContext.Database.MigrateAsync();
        }

        Guid tipoIncidenteId;

        await using (var dbContext = new AppDbContext(options))
        {
            var handler = new CrearTipoIncidenteCommandHandler(dbContext);
            tipoIncidenteId = await handler.Handle(
                new CrearTipoIncidenteCommand("25", "Persona sospechosa"), CancellationToken.None);
        }

        await using var assertContext = new AppDbContext(options);
        var tipoIncidente = await assertContext.TiposIncidente.FindAsync(tipoIncidenteId);

        Assert.NotNull(tipoIncidente);
        Assert.Equal("25", tipoIncidente!.Codigo);
        Assert.Equal("Persona sospechosa", tipoIncidente.Descripcion);
    }
}
