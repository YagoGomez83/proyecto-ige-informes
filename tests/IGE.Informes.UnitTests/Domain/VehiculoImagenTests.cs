using IGE.Informes.Domain.Entities;

namespace IGE.Informes.UnitTests.Domain;

public class VehiculoImagenTests
{
    [Fact]
    public void Alta_valida_asigna_id_y_fecha_de_carga()
    {
        var vehiculoId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();

        var imagen = new VehiculoImagen(vehiculoId, "clave/foto.jpg", usuarioId);

        Assert.NotEqual(Guid.Empty, imagen.Id);
        Assert.Equal(vehiculoId, imagen.VehiculoId);
        Assert.Equal("clave/foto.jpg", imagen.ImagenPath);
        Assert.Equal(usuarioId, imagen.SubidaPorUsuarioId);
        Assert.True(imagen.FechaCarga <= DateTime.UtcNow);
    }

    [Fact]
    public void Alta_rechaza_vehiculo_vacio()
    {
        Assert.Throws<ArgumentException>(() => new VehiculoImagen(Guid.Empty, "clave/foto.jpg", Guid.NewGuid()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Alta_rechaza_ruta_de_imagen_vacia_o_en_blanco(string imagenPath)
    {
        Assert.Throws<ArgumentException>(() => new VehiculoImagen(Guid.NewGuid(), imagenPath, Guid.NewGuid()));
    }

    [Fact]
    public void Alta_rechaza_usuario_vacio()
    {
        Assert.Throws<ArgumentException>(() => new VehiculoImagen(Guid.NewGuid(), "clave/foto.jpg", Guid.Empty));
    }
}
