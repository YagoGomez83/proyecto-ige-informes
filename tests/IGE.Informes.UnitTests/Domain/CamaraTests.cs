using IGE.Informes.Domain.Entities;

namespace IGE.Informes.UnitTests.Domain;

public class CamaraTests
{
    [Fact]
    public void Alta_valida_asigna_id_y_datos()
    {
        var camara = new Camara("SL 18", TipoCamara.Domo, "Av. Illia y San Martín");

        Assert.NotEqual(Guid.Empty, camara.Id);
        Assert.Equal("SL 18", camara.Codigo);
        Assert.Equal(TipoCamara.Domo, camara.Tipo);
        Assert.Equal("Av. Illia y San Martín", camara.Ubicacion);
    }

    [Fact]
    public void Alta_sin_ubicacion_se_acepta_pendiente_de_completar()
    {
        var camara = new Camara("JK 51", TipoCamara.Lpr);

        Assert.Null(camara.Ubicacion);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Alta_rechaza_codigo_vacio_o_en_blanco(string codigo)
    {
        Assert.Throws<ArgumentException>(() => new Camara(codigo, TipoCamara.Domo));
    }

    [Fact]
    public void CompletarUbicacion_completa_una_camara_pendiente()
    {
        var camara = new Camara("JK 51", TipoCamara.Lpr);

        camara.CompletarUbicacion("Ruta 7 km 12");

        Assert.Equal("Ruta 7 km 12", camara.Ubicacion);
    }

    [Fact]
    public void CompletarUbicacion_rechaza_ubicacion_vacia()
    {
        var camara = new Camara("JK 51", TipoCamara.Lpr);

        Assert.Throws<ArgumentException>(() => camara.CompletarUbicacion(""));
    }

    [Fact]
    public void CambiarTipo_actualiza_el_tipo()
    {
        var camara = new Camara("SL 18", TipoCamara.Domo);

        camara.CambiarTipo(TipoCamara.Lpr);

        Assert.Equal(TipoCamara.Lpr, camara.Tipo);
    }
}
