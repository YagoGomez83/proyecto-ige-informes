using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Application.Personas.Queries.ListarPersonas;
using IGE.Informes.Domain.Entities;
using IGE.Informes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace IGE.Informes.IntegrationTests.Personas;

/// <summary>
/// El orden usa una expresión condicional sobre Nombre nulo
/// (p.Nombre == null ? 1 : 0) — no traduce igual en EF Core InMemory que
/// en Npgsql real, mismo motivo que ListarVehiculosOrdenTests.
/// </summary>
public class ListarPersonasOrdenTests : IAsyncLifetime
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
    public async Task ListarPersonas_OrdenaIdentificadasPrimero_LuegoRolAlfabetico_LuegoNombreAlfabetico()
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
            var sinIdentificar = new Persona(RolPersona.Testigo, caracteristicas: "Contextura media, campera oscura");
            var zetaSospechoso = new Persona(RolPersona.Sospechoso, "Zeta");
            var alfaSospechoso = new Persona(RolPersona.Sospechoso, "Alfa");
            var betaTestigo = new Persona(RolPersona.Testigo, "Beta");

            // Orden de inserción deliberadamente distinto al orden esperado.
            setupContext.Personas.AddRange(sinIdentificar, betaTestigo, zetaSospechoso, alfaSospechoso);
            await setupContext.SaveChangesAsync();
        }

        await using var dbContext = new AppDbContext(options);
        var handler = new ListarPersonasQueryHandler(dbContext, new NullAuditLogger());

        var resultado = await handler.Handle(new ListarPersonasQuery(Pagina: 1, TamanioPagina: 50), CancellationToken.None);

        var nombres = resultado.Items.Select(p => p.Nombre).ToList();

        // Identificadas primero: Sospechoso (Alfa, Zeta alfabético) antes que
        // Testigo (Beta) — Rol alfabético entre grupos identificados;
        // Sin identificar (null) al final.
        Assert.Equal(["Alfa", "Zeta", "Beta", null], nombres);
    }
}
