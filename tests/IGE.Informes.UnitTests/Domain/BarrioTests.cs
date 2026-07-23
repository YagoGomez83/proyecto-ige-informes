using IGE.Informes.Domain.Entities;

namespace IGE.Informes.UnitTests.Domain;

public class BarrioTests
{
    [Fact]
    public void Alta_valida_asigna_id_y_nombre()
    {
        var barrio = new Barrio("Barrio Norte");

        Assert.NotEqual(Guid.Empty, barrio.Id);
        Assert.Equal("Barrio Norte", barrio.Nombre);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Alta_rechaza_nombre_vacio_o_en_blanco(string nombre)
    {
        Assert.Throws<ArgumentException>(() => new Barrio(nombre));
    }
}
