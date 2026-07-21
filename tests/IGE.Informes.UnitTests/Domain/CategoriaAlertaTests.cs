using IGE.Informes.Domain.Entities;

namespace IGE.Informes.UnitTests.Domain;

public class CategoriaAlertaTests
{
    [Fact]
    public void Alta_valida_asigna_id_y_nombre()
    {
        var categoria = new CategoriaAlerta("Robado");

        Assert.NotEqual(Guid.Empty, categoria.Id);
        Assert.Equal("Robado", categoria.Nombre);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Alta_rechaza_nombre_vacio_o_en_blanco(string nombre)
    {
        Assert.Throws<ArgumentException>(() => new CategoriaAlerta(nombre));
    }
}
