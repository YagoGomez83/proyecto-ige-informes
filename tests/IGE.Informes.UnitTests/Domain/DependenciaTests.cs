using IGE.Informes.Domain.Entities;

namespace IGE.Informes.UnitTests.Domain;

public class DependenciaTests
{
    [Fact]
    public void Alta_valida_asigna_id_y_datos()
    {
        var dependencia = new Dependencia("Comisaría 2°", TipoDependencia.Comisaria);

        Assert.NotEqual(Guid.Empty, dependencia.Id);
        Assert.Equal("Comisaría 2°", dependencia.Nombre);
        Assert.Equal(TipoDependencia.Comisaria, dependencia.Tipo);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Alta_rechaza_nombre_vacio_o_en_blanco(string nombre)
    {
        Assert.Throws<ArgumentException>(() => new Dependencia(nombre, TipoDependencia.Fiscalia));
    }
}
