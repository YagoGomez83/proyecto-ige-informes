using IGE.Informes.Application.Vehiculos.Commands.AgregarImagenVehiculo;

namespace IGE.Informes.UnitTests.Vehiculos;

public class AgregarImagenVehiculoCommandValidatorTests
{
    private static readonly byte[] JpegValido = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];

    [Fact]
    public void Acepta_una_imagen_jpeg_valida()
    {
        var validator = new AgregarImagenVehiculoCommandValidator();

        var resultado = validator.Validate(new AgregarImagenVehiculoCommand(Guid.NewGuid(), JpegValido, "foto.jpg", "image/jpeg"));

        Assert.True(resultado.IsValid);
    }

    [Fact]
    public void Rechaza_un_tipo_mime_no_soportado()
    {
        var validator = new AgregarImagenVehiculoCommandValidator();

        var resultado = validator.Validate(new AgregarImagenVehiculoCommand(Guid.NewGuid(), JpegValido, "foto.gif", "image/gif"));

        Assert.False(resultado.IsValid);
    }

    [Fact]
    public void Rechaza_contenido_que_no_coincide_con_el_tipo_mime_declarado()
    {
        // Content-Type falseado: el cliente declara "image/jpeg" pero el
        // contenido real no tiene la firma de un JPEG — ver
        // FormatoImagenHelperTests para la validación de magic bytes en sí.
        var contenidoFalso = new byte[] { 0x4D, 0x5A, 0x90, 0x00 };
        var validator = new AgregarImagenVehiculoCommandValidator();

        var resultado = validator.Validate(new AgregarImagenVehiculoCommand(Guid.NewGuid(), contenidoFalso, "foto.jpg", "image/jpeg"));

        Assert.False(resultado.IsValid);
    }

    [Fact]
    public void Rechaza_contenido_vacio()
    {
        var validator = new AgregarImagenVehiculoCommandValidator();

        var resultado = validator.Validate(new AgregarImagenVehiculoCommand(Guid.NewGuid(), [], "foto.jpg", "image/jpeg"));

        Assert.False(resultado.IsValid);
    }

    [Fact]
    public void Rechaza_contenido_que_supera_el_tamanio_maximo()
    {
        var contenidoGrande = new byte[10 * 1024 * 1024 + 1];
        Array.Copy(JpegValido, contenidoGrande, JpegValido.Length);
        var validator = new AgregarImagenVehiculoCommandValidator();

        var resultado = validator.Validate(new AgregarImagenVehiculoCommand(Guid.NewGuid(), contenidoGrande, "foto.jpg", "image/jpeg"));

        Assert.False(resultado.IsValid);
    }

    [Fact]
    public void Rechaza_vehiculo_vacio()
    {
        var validator = new AgregarImagenVehiculoCommandValidator();

        var resultado = validator.Validate(new AgregarImagenVehiculoCommand(Guid.Empty, JpegValido, "foto.jpg", "image/jpeg"));

        Assert.False(resultado.IsValid);
    }
}
