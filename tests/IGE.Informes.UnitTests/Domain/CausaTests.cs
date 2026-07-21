using IGE.Informes.Domain.Entities;

namespace IGE.Informes.UnitTests.Domain;

public class CausaTests
{
    [Fact]
    public void Alta_valida_asigna_id_y_datos()
    {
        var causa = new Causa("N.N. s/Robo", "7070029/26", "Primera Circunscripción");

        Assert.NotEqual(Guid.Empty, causa.Id);
        Assert.Equal("N.N. s/Robo", causa.Caratula);
        Assert.Equal("7070029/26", causa.NroPiezaSumarial);
        Assert.Equal("Primera Circunscripción", causa.CircunscripcionJudicial);
    }

    [Theory]
    [InlineData("", "7070029/26", "Primera Circunscripción")]
    [InlineData("N.N. s/Robo", "", "Primera Circunscripción")]
    [InlineData("N.N. s/Robo", "7070029/26", "")]
    public void Alta_rechaza_campos_obligatorios_vacios(string caratula, string pieza, string circunscripcion)
    {
        Assert.Throws<ArgumentException>(() => new Causa(caratula, pieza, circunscripcion));
    }
}
