using IGE.Informes.Domain.Entities;

namespace IGE.Informes.UnitTests.Domain;

public class TipoIncidenteTests
{
    [Fact]
    public void Alta_valida_asigna_id_y_datos()
    {
        var tipoIncidente = new TipoIncidente("164", "ROBO");

        Assert.NotEqual(Guid.Empty, tipoIncidente.Id);
        Assert.Equal("164", tipoIncidente.Codigo);
        Assert.Equal("ROBO", tipoIncidente.Descripcion);
    }

    [Theory]
    [InlineData("", "ROBO")]
    [InlineData("164", "")]
    public void Alta_rechaza_codigo_o_descripcion_vacios(string codigo, string descripcion)
    {
        Assert.Throws<ArgumentException>(() => new TipoIncidente(codigo, descripcion));
    }
}
