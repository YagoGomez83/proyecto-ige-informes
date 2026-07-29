using IGE.Informes.Application.Common.Exceptions;
using IGE.Informes.Application.TiposIncidente.Commands.CrearTipoIncidente;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.TiposIncidente;

/// <summary>
/// HU-18 (Catálogo de Tipos de Incidente) — cubre el Handler de
/// CrearTipoIncidenteCommand, mismo patrón que CrearBarrioCommandHandlerTests
/// (chequeo de duplicado en memoria antes del SaveChanges). El constraint
/// único a nivel de DB real (índice IX_TiposIncidente_Codigo) se cubre aparte
/// con Testcontainers, ya que EF Core InMemory no lo aplica.
/// </summary>
public class CrearTipoIncidenteCommandHandlerTests
{
    [Fact]
    public async Task Registra_un_tipo_de_incidente_nuevo()
    {
        var dbContext = new TestAppDbContext();
        var handler = new CrearTipoIncidenteCommandHandler(dbContext);

        var tipoIncidenteId = await handler.Handle(
            new CrearTipoIncidenteCommand("25", "Persona sospechosa"), CancellationToken.None);

        var tipoIncidente = await dbContext.TiposIncidente.FindAsync(tipoIncidenteId);
        Assert.NotNull(tipoIncidente);
        Assert.Equal("25", tipoIncidente!.Codigo);
        Assert.Equal("Persona sospechosa", tipoIncidente.Descripcion);
    }

    [Fact]
    public async Task Rechaza_un_codigo_duplicado()
    {
        var dbContext = new TestAppDbContext();
        dbContext.TiposIncidente.Add(new TipoIncidente("25", "Persona sospechosa"));
        await dbContext.SaveChangesAsync();

        var handler = new CrearTipoIncidenteCommandHandler(dbContext);

        await Assert.ThrowsAsync<EntidadDuplicadaException>(() => handler.Handle(
            new CrearTipoIncidenteCommand("25", "Asalto a mano armada"), CancellationToken.None));
    }

    [Fact]
    public async Task Rechaza_un_codigo_duplicado_aunque_la_descripcion_sea_distinta()
    {
        var dbContext = new TestAppDbContext();
        dbContext.TiposIncidente.Add(new TipoIncidente("02", "Asalto a mano armada"));
        await dbContext.SaveChangesAsync();

        var handler = new CrearTipoIncidenteCommandHandler(dbContext);

        var excepcion = await Assert.ThrowsAsync<EntidadDuplicadaException>(() => handler.Handle(
            new CrearTipoIncidenteCommand("02", "Otra descripción cualquiera"), CancellationToken.None));

        Assert.Contains("02", excepcion.Message);
    }
}
