using IGE.Informes.Application.CasosAnalisis.Queries.ObtenerTableroAnalitica;
using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Domain.Entities;
using IGE.Informes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace IGE.Informes.IntegrationTests.CasosAnalisis;

/// <summary>
/// HU-06 · Tablero de analítica de gestión (Épica 02), contra Postgres real
/// vía Testcontainers — el GroupBy con 4 dimensiones (Dependencia,
/// TipoIncidente, Analista vía CasoAnalista, Resultado) debe traducir
/// correctamente a SQL real, no solo funcionar contra EF Core InMemory
/// (mismo criterio que BuscarInformesDominioVehiculoTests: "EF Core
/// InMemory no es fiel a Postgres real para features específicas del
/// motor" — ver memoria del proyecto). En particular, el conteo por
/// Analista requiere un GroupBy sobre la owned collection CasoAnalista, que
/// se resuelve distinto entre providers.
///
/// Todavía NO existe ObtenerTableroAnaliticaQuery/Handler en producción —
/// este test es la especificación ejecutable (debe fallar en rojo por
/// ausencia de implementación / error de compilación) hasta que se
/// implemente.
/// </summary>
public class ObtenerTableroAnaliticaTests : IAsyncLifetime
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
    public async Task ObtenerTableroAnalitica_ConCasosReales_CuentaCorrectamentePorLasCuatroDimensionesContraPostgresReal()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using (var migrationContext = new AppDbContext(options))
        {
            await migrationContext.Database.MigrateAsync();
        }

        Guid dependenciaComisaria1, tipoRobo, tipoHurto, analistaCreador, analistaOperador;

        await using (var setupContext = new AppDbContext(options))
        {
            var dependencia1 = new Dependencia("Comisaría Seccional Primera", TipoDependencia.Comisaria);
            var dependencia2 = new Dependencia("Comisaría Seccional Segunda", TipoDependencia.Comisaria);
            setupContext.Dependencias.AddRange(dependencia1, dependencia2);

            var incidenteRobo = new TipoIncidente("164", "ROBO");
            var incidenteHurto = new TipoIncidente("162", "HURTO");
            setupContext.TiposIncidente.AddRange(incidenteRobo, incidenteHurto);
            await setupContext.SaveChangesAsync();

            dependenciaComisaria1 = dependencia1.Id;
            tipoRobo = incidenteRobo.Id;
            tipoHurto = incidenteHurto.Id;
            analistaCreador = Guid.NewGuid();
            analistaOperador = Guid.NewGuid();

            var casoUno = new CasoAnalisis(new DateOnly(2026, 5, 10), dependencia1.Id, incidenteRobo.Id, analistaCreador);
            casoUno.AsignarAnalista(analistaOperador, RolCasoAnalista.Operador);
            casoUno.CerrarConResultado(ResultadoCaso.Positivo);

            var casoDos = new CasoAnalisis(new DateOnly(2026, 5, 20), dependencia1.Id, incidenteHurto.Id, analistaCreador);
            casoDos.CerrarConResultado(ResultadoCaso.Negativo);

            var casoTres = new CasoAnalisis(new DateOnly(2026, 6, 1), dependencia2.Id, incidenteRobo.Id, analistaOperador);
            // Fuera del rango de fechas que se va a consultar (mayo 2026).

            setupContext.CasosAnalisis.AddRange(casoUno, casoDos, casoTres);
            await setupContext.SaveChangesAsync();
        }

        await using var dbContext = new AppDbContext(options);
        var handler = new ObtenerTableroAnaliticaQueryHandler(dbContext, new NullAuditLogger());

        var resultado = await handler.Handle(
            new ObtenerTableroAnaliticaQuery(
                FechaDesde: new DateOnly(2026, 5, 1),
                FechaHasta: new DateOnly(2026, 5, 31)),
            CancellationToken.None);

        Assert.Equal(2, resultado.PorDependencia.Single(c => c.DependenciaId == dependenciaComisaria1).Cantidad);

        Assert.Contains(resultado.PorTipoIncidente, c => c.TipoIncidenteId == tipoRobo && c.Cantidad == 1);
        Assert.Contains(resultado.PorTipoIncidente, c => c.TipoIncidenteId == tipoHurto && c.Cantidad == 1);

        Assert.Contains(resultado.PorAnalista, c => c.AnalistaUsuarioId == analistaCreador && c.Cantidad == 2);
        Assert.Contains(resultado.PorAnalista, c => c.AnalistaUsuarioId == analistaOperador && c.Cantidad == 1);

        Assert.Contains(resultado.PorResultado, c => c.Resultado == ResultadoCaso.Positivo && c.Cantidad == 1);
        Assert.Contains(resultado.PorResultado, c => c.Resultado == ResultadoCaso.Negativo && c.Cantidad == 1);
    }
}
