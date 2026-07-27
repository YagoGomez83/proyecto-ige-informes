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

    [Fact]
    public void Alta_con_dependencia_la_asigna()
    {
        var dependenciaId = Guid.NewGuid();

        var camara = new Camara("SL 18", TipoCamara.Domo, dependenciaId: dependenciaId);

        Assert.Equal(dependenciaId, camara.DependenciaId);
    }

    [Fact]
    public void Alta_sin_dependencia_queda_sin_jurisdiccion()
    {
        var camara = new Camara("LP 217", TipoCamara.Lpr);

        Assert.Null(camara.DependenciaId);
    }

    [Fact]
    public void AsignarDependencia_vincula_la_camara_a_una_dependencia()
    {
        var camara = new Camara("LP 217", TipoCamara.Lpr);
        var dependenciaId = Guid.NewGuid();

        camara.AsignarDependencia(dependenciaId);

        Assert.Equal(dependenciaId, camara.DependenciaId);
    }

    [Fact]
    public void AsignarDependencia_rechaza_id_vacio()
    {
        var camara = new Camara("LP 217", TipoCamara.Lpr);

        Assert.Throws<ArgumentException>(() => camara.AsignarDependencia(Guid.Empty));
    }

    [Fact]
    public void QuitarDependencia_deja_la_camara_sin_jurisdiccion()
    {
        var camara = new Camara("SL 18", TipoCamara.Domo, dependenciaId: Guid.NewGuid());

        camara.QuitarDependencia();

        Assert.Null(camara.DependenciaId);
    }

    [Fact]
    public void Alta_sin_localidad_ni_centro_control_camaras_quedan_null_por_defecto()
    {
        var camara = new Camara("SL 18", TipoCamara.Domo);

        Assert.Null(camara.LocalidadId);
        Assert.Null(camara.CentroControlCamarasId);
    }

    [Fact]
    public void AsignarLocalidad_vincula_la_camara_a_una_localidad()
    {
        var camara = new Camara("SL 18", TipoCamara.Domo);
        var localidadId = Guid.NewGuid();

        camara.AsignarLocalidad(localidadId);

        Assert.Equal(localidadId, camara.LocalidadId);
    }

    [Fact]
    public void AsignarLocalidad_rechaza_id_vacio()
    {
        var camara = new Camara("SL 18", TipoCamara.Domo);

        Assert.Throws<ArgumentException>(() => camara.AsignarLocalidad(Guid.Empty));
    }

    [Fact]
    public void AsignarCentroControlCamaras_vincula_la_camara_a_un_centro_de_control()
    {
        var camara = new Camara("SL 18", TipoCamara.Domo);
        var centroControlCamarasId = Guid.NewGuid();

        camara.AsignarCentroControlCamaras(centroControlCamarasId);

        Assert.Equal(centroControlCamarasId, camara.CentroControlCamarasId);
    }

    [Fact]
    public void AsignarCentroControlCamaras_rechaza_id_vacio()
    {
        var camara = new Camara("SL 18", TipoCamara.Domo);

        Assert.Throws<ArgumentException>(() => camara.AsignarCentroControlCamaras(Guid.Empty));
    }
}
