using IGE.Informes.Application;
using IGE.Informes.Application.CasosAnalisis.Queries.BuscarCasos;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Application.Personas.Queries.BuscarPersonas;
using IGE.Informes.Application.Vehiculos.Queries.BuscarVehiculos;
using IGE.Informes.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace IGE.Informes.IntegrationTests.Busqueda;

/// <summary>
/// Los Handlers de búsqueda que viven en Infrastructure (no en Application,
/// por EF.Functions.ILike/ToTsVector) no se descubren por el escaneo de
/// ensamblado de MediatR (services.AddMediatR solo escanea Application) —
/// hay que registrarlos a mano en Infrastructure/DependencyInjection.cs.
/// Un Handler nuevo que se olvide de ese registro compila sin errores, pasa
/// los tests unitarios (instancian el Handler directo con `new`) y los de
/// integración existentes (mismo patrón) — el único síntoma real es
/// "Handler not found" al ejecutar Sender.Send de verdad en producción,
/// silenciado además por cualquier catch (Exception) genérico en la UI.
/// Este test resuelve el ISender real (con el mismo AddApplication +
/// AddInfrastructure que usa Program.cs) para que un Handler nuevo sin
/// registrar rompa acá, no en producción.
/// </summary>
public class RegistroDeHandlersDeBusquedaTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public Guid? UsuarioId => Guid.NewGuid();
        public IReadOnlyCollection<string> Roles => [IGE.Informes.Application.Common.Security.Roles.Admin];
    }

    private ServiceProvider? _serviceProvider;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _postgres.GetConnectionString(),
            })
            .Build();

        var services = new ServiceCollection()
            .AddLogging()
            .AddApplication()
            .AddInfrastructure(configuration);

        services.AddScoped<ICurrentUserService, FakeCurrentUserService>();

        _serviceProvider = services.BuildServiceProvider();

        await using var scope = _serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IGE.Informes.Infrastructure.Persistence.AppDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_serviceProvider is not null)
        {
            await _serviceProvider.DisposeAsync();
        }

        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task BuscarVehiculosQuery_TieneHandlerRegistradoEnElContenedorReal()
    {
        await using var scope = _serviceProvider!.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var resultado = await sender.Send(new BuscarVehiculosQuery("ABC"));

        Assert.NotNull(resultado);
    }

    [Fact]
    public async Task BuscarPersonasQuery_TieneHandlerRegistradoEnElContenedorReal()
    {
        await using var scope = _serviceProvider!.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var resultado = await sender.Send(new BuscarPersonasQuery("ABC"));

        Assert.NotNull(resultado);
    }

    [Fact]
    public async Task BuscarCasosQuery_TieneHandlerRegistradoEnElContenedorReal()
    {
        await using var scope = _serviceProvider!.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var resultado = await sender.Send(new BuscarCasosQuery("ABC"));

        Assert.NotNull(resultado);
    }
}
