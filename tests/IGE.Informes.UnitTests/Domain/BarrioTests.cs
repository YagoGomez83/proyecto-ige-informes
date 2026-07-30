namespace IGE.Informes.UnitTests.Domain;

using IGE.Informes.Domain.Entities;

public class BarrioTests
{
    [Fact]
    public void Alta_valida_asigna_id_y_nombre_sin_localidad()
    {
        var barrio = new Barrio("Barrio Norte");

        Assert.NotEqual(Guid.Empty, barrio.Id);
        Assert.Equal("Barrio Norte", barrio.Nombre);
        Assert.Null(barrio.LocalidadId);
    }

    [Fact]
    public void Alta_valida_con_localidad_asigna_el_localidadId()
    {
        var localidadId = Guid.NewGuid();

        var barrio = new Barrio("Barrio Norte", localidadId);

        Assert.Equal("Barrio Norte", barrio.Nombre);
        Assert.Equal(localidadId, barrio.LocalidadId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Alta_rechaza_nombre_vacio_o_en_blanco(string nombre)
    {
        Assert.Throws<ArgumentException>(() => new Barrio(nombre));
    }
}
