using IGE.Informes.Application.Common.Interfaces;
using IGE.Informes.Application.Informes.Commands.GenerarInformeDesdeCaso;
using IGE.Informes.Domain.Entities;
using IGE.Informes.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace IGE.Informes.IntegrationTests.Informes;

public class GenerarInformeDesdeCasoTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private sealed class FakeCurrentUserService(Guid usuarioId) : ICurrentUserService
    {
        public Guid? UsuarioId { get; } = usuarioId;
        public IReadOnlyCollection<string> Roles { get; } = ["Analista"];
    }

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact]
    public async Task Genera_dos_Informes_consecutivos_con_correlativo_incremental_contra_Postgres_real()
    {
        var currentUserService = new FakeCurrentUserService(Guid.NewGuid());

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using (var migrationContext = new AppDbContext(options))
        {
            await migrationContext.Database.MigrateAsync();
        }

        Guid casoId;
        Guid dependenciaId;

        await using (var setupContext = new AppDbContext(options))
        {
            var caso = new CasoAnalisis(new DateOnly(2026, 7, 21), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            var dependencia = new Dependencia("Fiscalía N°3", TipoDependencia.Fiscalia);
            setupContext.CasosAnalisis.Add(caso);
            setupContext.Dependencias.Add(dependencia);
            await setupContext.SaveChangesAsync();

            casoId = caso.Id;
            dependenciaId = dependencia.Id;
        }

        string primerIdRegistro;
        string segundoIdRegistro;

        await using (var dbContext = new AppDbContext(options))
        {
            var handler = new GenerarInformeDesdeCasoCommandHandler(dbContext, currentUserService);

            var primerInformeId = await handler.Handle(
                new GenerarInformeDesdeCasoCommand(casoId, dependenciaId, "N.N. s/Robo", "7070029/26", "Primera Circunscripción"),
                CancellationToken.None);

            primerIdRegistro = (await dbContext.Informes.FindAsync(primerInformeId))!.IdRegistro;
        }

        await using (var dbContext = new AppDbContext(options))
        {
            var handler = new GenerarInformeDesdeCasoCommandHandler(dbContext, currentUserService);

            var segundoInformeId = await handler.Handle(
                new GenerarInformeDesdeCasoCommand(casoId, dependenciaId, "N.N. s/Robo (2)", "7070030/26", "Primera Circunscripción"),
                CancellationToken.None);

            segundoIdRegistro = (await dbContext.Informes.FindAsync(segundoInformeId))!.IdRegistro;
        }

        Assert.Equal($"1/{DateTime.UtcNow.Year}", primerIdRegistro);
        Assert.Equal($"2/{DateTime.UtcNow.Year}", segundoIdRegistro);
    }
}
