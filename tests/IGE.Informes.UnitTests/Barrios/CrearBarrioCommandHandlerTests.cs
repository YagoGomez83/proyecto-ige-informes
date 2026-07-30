using IGE.Informes.Application.Barrios.Commands.CrearBarrio;
using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;
using Microsoft.EntityFrameworkCore;

namespace IGE.Informes.UnitTests.Barrios;

/// <summary>
/// HU-13 (Catálogo de Barrios) — extensión 2026-07-29: la unicidad de
/// Barrio.Nombre pasa de ser global a ser compuesta con LocalidadId
/// (nullable), para permitir el mismo nombre de Barrio en Localidades
/// distintas.
/// </summary>
public class CrearBarrioCommandHandlerTests
{
    [Fact]
    public async Task Registra_un_barrio_nuevo_con_localidad()
    {
        var dbContext = new TestAppDbContext();
        var localidad = new Localidad("San Luis");
        dbContext.Localidades.Add(localidad);
        await dbContext.SaveChangesAsync();

        var handler = new CrearBarrioCommandHandler(dbContext);

        var barrioId = await handler.Handle(
            new CrearBarrioCommand("Barrio Norte", localidad.Id), CancellationToken.None);

        var barrio = await dbContext.Barrios.FindAsync(barrioId);
        Assert.Equal("Barrio Norte", barrio!.Nombre);
        Assert.Equal(localidad.Id, barrio.LocalidadId);
    }

    [Fact]
    public async Task Registra_un_barrio_nuevo_sin_localidad()
    {
        var dbContext = new TestAppDbContext();
        var handler = new CrearBarrioCommandHandler(dbContext);

        var barrioId = await handler.Handle(
            new CrearBarrioCommand("Barrio Norte", null), CancellationToken.None);

        var barrio = await dbContext.Barrios.FindAsync(barrioId);
        Assert.Equal("Barrio Norte", barrio!.Nombre);
        Assert.Null(barrio.LocalidadId);
    }

    [Fact]
    public async Task Rechaza_un_nombre_duplicado_dentro_de_la_misma_localidad()
    {
        var dbContext = new TestAppDbContext();
        var localidad = new Localidad("San Luis");
        dbContext.Localidades.Add(localidad);
        dbContext.Barrios.Add(new Barrio("Barrio Norte", localidad.Id));
        await dbContext.SaveChangesAsync();

        var handler = new CrearBarrioCommandHandler(dbContext);

        await Assert.ThrowsAsync<EntidadDuplicadaException>(() => handler.Handle(
            new CrearBarrioCommand("Barrio Norte", localidad.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Permite_el_mismo_nombre_en_localidades_distintas()
    {
        var dbContext = new TestAppDbContext();
        var sanLuis = new Localidad("San Luis");
        var villaMercedes = new Localidad("Villa Mercedes");
        dbContext.Localidades.Add(sanLuis);
        dbContext.Localidades.Add(villaMercedes);
        dbContext.Barrios.Add(new Barrio("Barrio Norte", sanLuis.Id));
        await dbContext.SaveChangesAsync();

        var handler = new CrearBarrioCommandHandler(dbContext);

        var barrioId = await handler.Handle(
            new CrearBarrioCommand("Barrio Norte", villaMercedes.Id), CancellationToken.None);

        var barrio = await dbContext.Barrios.FindAsync(barrioId);
        Assert.Equal(villaMercedes.Id, barrio!.LocalidadId);
    }

    [Fact]
    public async Task Permite_el_mismo_nombre_sin_localidad_en_ambos_casos()
    {
        var dbContext = new TestAppDbContext();
        dbContext.Barrios.Add(new Barrio("Barrio Norte", null));
        await dbContext.SaveChangesAsync();

        var handler = new CrearBarrioCommandHandler(dbContext);

        var barrioId = await handler.Handle(
            new CrearBarrioCommand("Barrio Norte", null), CancellationToken.None);

        Assert.NotEqual(Guid.Empty, barrioId);
        Assert.Equal(2, await dbContext.Barrios.CountAsync());
    }
}
