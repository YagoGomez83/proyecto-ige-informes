using IGE.Informes.Application.Common.Validation;

namespace IGE.Informes.UnitTests.Common;

public class FormatoImagenHelperTests
{
    private static readonly byte[] JpegValido = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];
    private static readonly byte[] PngValido = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00];
    private static readonly byte[] WebpValido = [0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50];
    private static readonly byte[] NoEsImagen = [0x4D, 0x5A, 0x90, 0x00]; // cabecera de un .exe (MZ)

    [Fact]
    public void Jpeg_valido_coincide_con_tipo_declarado()
    {
        Assert.True(FormatoImagenHelper.CoincideConTipoDeclarado(JpegValido, "image/jpeg"));
    }

    [Fact]
    public void Png_valido_coincide_con_tipo_declarado()
    {
        Assert.True(FormatoImagenHelper.CoincideConTipoDeclarado(PngValido, "image/png"));
    }

    [Fact]
    public void Webp_valido_coincide_con_tipo_declarado()
    {
        Assert.True(FormatoImagenHelper.CoincideConTipoDeclarado(WebpValido, "image/webp"));
    }

    [Fact]
    public void Contenido_no_imagen_con_content_type_falso_no_coincide()
    {
        Assert.False(FormatoImagenHelper.CoincideConTipoDeclarado(NoEsImagen, "image/jpeg"));
    }

    [Fact]
    public void Png_declarado_como_jpeg_no_coincide()
    {
        Assert.False(FormatoImagenHelper.CoincideConTipoDeclarado(PngValido, "image/jpeg"));
    }

    [Fact]
    public void Tipo_mime_no_soportado_nunca_coincide()
    {
        Assert.False(FormatoImagenHelper.CoincideConTipoDeclarado(JpegValido, "application/pdf"));
    }
}
