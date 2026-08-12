using IGE.Informes.Application.Common.Services;
using IGE.Informes.Domain.Entities;
using IGE.Informes.UnitTests.TestDoubles;

namespace IGE.Informes.UnitTests.Common.Services;

/// <summary>
/// docs/03-modelo-dominio.md, entrada "Causa.NroPiezaSumarial pasa a ser
/// opcional" — bug real: dos Informes sin N° de Pieza Sumarial (placeholder
/// "--/--") terminaban compartiendo la misma Causa porque
/// BuscarPorPiezaSumarialAsync matcheaba por igualdad exacta, y null/""
/// también "matcheaban" entre sí. Sin número no hay forma confiable de
/// saber si es el mismo expediente — cada Informe sin Pieza Sumarial debe
/// recibir su propia Causa nueva, nunca reutilizar la de otro Informe.
/// </summary>
public class CausaMatcherTests
{
    [Fact]
    public async Task ObtenerOCrearIdAsync_DosVecesConNroPiezaSumarialNuloYCaratulasDistintas_CreaDosCausasDistintas()
    {
        var dbContext = new TestAppDbContext();

        var primerCausaId = await CausaMatcher.ObtenerOCrearIdAsync(
            dbContext, "AV. INFRACCIÓN LEY 23.737", null, null, CancellationToken.None);
        await dbContext.SaveChangesAsync();

        var segundaCausaId = await CausaMatcher.ObtenerOCrearIdAsync(
            dbContext, "TAREA INVESTIGATIVA", null, null, CancellationToken.None);
        await dbContext.SaveChangesAsync();

        Assert.NotEqual(primerCausaId, segundaCausaId);
        Assert.Equal(2, dbContext.Causas.Count());

        var primeraCausa = await dbContext.Causas.FindAsync(primerCausaId);
        var segundaCausa = await dbContext.Causas.FindAsync(segundaCausaId);
        Assert.NotNull(primeraCausa);
        Assert.NotNull(segundaCausa);
        Assert.Equal("AV. INFRACCIÓN LEY 23.737", primeraCausa.Caratula);
        Assert.Equal("TAREA INVESTIGATIVA", segundaCausa.Caratula);
        Assert.Null(primeraCausa.NroPiezaSumarial);
        Assert.Null(segundaCausa.NroPiezaSumarial);
    }

    [Fact]
    public async Task ObtenerOCrearIdAsync_DosVecesConMismoNroPiezaSumarialReal_ReutilizaLaMismaCausa()
    {
        // Comportamiento viejo intacto: el matching por N° de Pieza Sumarial
        // exacto sigue siendo correcto cuando el número es real (no null).
        var dbContext = new TestAppDbContext();

        var primerCausaId = await CausaMatcher.ObtenerOCrearIdAsync(
            dbContext, "N.N. s/Robo", "7070029/26", "Primera Circunscripción", CancellationToken.None);
        await dbContext.SaveChangesAsync();

        var segundaCausaId = await CausaMatcher.ObtenerOCrearIdAsync(
            dbContext, "N.N. s/Robo (otra transcripción)", "7070029/26", null, CancellationToken.None);
        await dbContext.SaveChangesAsync();

        Assert.Equal(primerCausaId, segundaCausaId);
        Assert.Single(dbContext.Causas);
    }

    [Fact]
    public async Task BuscarPorPiezaSumarialAsync_NroPiezaSumarialNulo_DevuelveNullSinConsultarLaBase()
    {
        var dbContext = new TestAppDbContext();
        var causaExistente = new Causa("TAREA INVESTIGATIVA", null, null);
        dbContext.Causas.Add(causaExistente);
        await dbContext.SaveChangesAsync();

        var resultado = await CausaMatcher.BuscarPorPiezaSumarialAsync(dbContext, null, CancellationToken.None);

        Assert.Null(resultado);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BuscarPorPiezaSumarialAsync_NroPiezaSumarialVacioOWhitespace_DevuelveNull(string nroPiezaSumarial)
    {
        var dbContext = new TestAppDbContext();

        var resultado = await CausaMatcher.BuscarPorPiezaSumarialAsync(dbContext, nroPiezaSumarial, CancellationToken.None);

        Assert.Null(resultado);
    }
}
