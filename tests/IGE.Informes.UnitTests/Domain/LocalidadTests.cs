using IGE.Informes.Domain.Entities;

namespace IGE.Informes.UnitTests.Domain;

public class LocalidadTests
{
    [Fact]
    public void Alta_valida_asigna_id_y_nombre()
    {
        var localidad = new Localidad("Estancia Grande");

        Assert.NotEqual(Guid.Empty, localidad.Id);
        Assert.Equal("Estancia Grande", localidad.Nombre);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Alta_rechaza_nombre_vacio_o_en_blanco(string nombre)
    {
        Assert.Throws<ArgumentException>(() => new Localidad(nombre));
    }
}
