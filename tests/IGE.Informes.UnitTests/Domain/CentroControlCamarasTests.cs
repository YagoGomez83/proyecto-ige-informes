using IGE.Informes.Domain.Entities;

namespace IGE.Informes.UnitTests.Domain;

public class CentroControlCamarasTests
{
    [Fact]
    public void Alta_valida_asigna_id_sigla_y_nombre()
    {
        var centroControlCamaras = new CentroControlCamaras("CCCSL", "Centro de Control de Cámaras San Luis");

        Assert.NotEqual(Guid.Empty, centroControlCamaras.Id);
        Assert.Equal("CCCSL", centroControlCamaras.Sigla);
        Assert.Equal("Centro de Control de Cámaras San Luis", centroControlCamaras.Nombre);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Alta_rechaza_sigla_vacia_o_en_blanco(string sigla)
    {
        Assert.Throws<ArgumentException>(() => new CentroControlCamaras(sigla, "Centro de Control de Cámaras San Luis"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Alta_rechaza_nombre_vacio_o_en_blanco(string nombre)
    {
        Assert.Throws<ArgumentException>(() => new CentroControlCamaras("CCCSL", nombre));
    }
}
