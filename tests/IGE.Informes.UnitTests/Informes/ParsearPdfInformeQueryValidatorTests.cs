using IGE.Informes.Application.Informes.Queries.ParsearPdfInforme;

namespace IGE.Informes.UnitTests.Informes;

public class ParsearPdfInformeQueryValidatorTests
{
    private readonly ParsearPdfInformeQueryValidator _validator = new();

    [Fact]
    public void Acepta_un_archivo_con_firma_PDF_real()
    {
        var contenido = "%PDF-1.4 resto del contenido"u8.ToArray();

        var resultado = _validator.Validate(new ParsearPdfInformeQuery(contenido));

        Assert.True(resultado.IsValid);
    }

    [Fact]
    public void Rechaza_un_archivo_sin_la_firma_de_PDF()
    {
        var contenido = "esto no es un pdf"u8.ToArray();

        var resultado = _validator.Validate(new ParsearPdfInformeQuery(contenido));

        Assert.False(resultado.IsValid);
    }

    [Fact]
    public void Rechaza_un_archivo_vacio()
    {
        var resultado = _validator.Validate(new ParsearPdfInformeQuery([]));

        Assert.False(resultado.IsValid);
    }

    [Fact]
    public void Rechaza_un_archivo_que_excede_el_tamanio_maximo()
    {
        var contenido = new byte[ParsearPdfInformeQueryValidator.TamanioMaximoBytes + 1];
        "%PDF-"u8.CopyTo(contenido);

        var resultado = _validator.Validate(new ParsearPdfInformeQuery(contenido));

        Assert.False(resultado.IsValid);
    }
}
