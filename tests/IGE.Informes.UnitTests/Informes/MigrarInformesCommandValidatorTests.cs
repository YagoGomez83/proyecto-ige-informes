using IGE.Informes.Application.Informes.Commands.MigrarInformes;

namespace IGE.Informes.UnitTests.Informes;

public class MigrarInformesCommandValidatorTests
{
    private readonly MigrarInformesCommandValidator _validator = new();
    private static readonly byte[] ContenidoPdfFalso = "%PDF-1.4 contenido de prueba"u8.ToArray();

    [Fact]
    public void Acepta_un_lote_dentro_del_limite()
    {
        var command = new MigrarInformesCommand(Guid.NewGuid(), [new PdfMigrarDto(ContenidoPdfFalso, "1.pdf")]);

        var resultado = _validator.Validate(command);

        Assert.True(resultado.IsValid);
    }

    [Fact]
    public void Rechaza_un_lote_que_excede_la_cantidad_maxima_de_archivos()
    {
        var pdfs = Enumerable.Range(1, MigrarInformesCommandValidator.CantidadMaximaArchivos + 1)
            .Select(i => new PdfMigrarDto(ContenidoPdfFalso, $"{i}.pdf"))
            .ToList();

        var command = new MigrarInformesCommand(Guid.NewGuid(), pdfs);

        var resultado = _validator.Validate(command);

        Assert.False(resultado.IsValid);
    }

    [Fact]
    public void Rechaza_un_lote_vacio()
    {
        var command = new MigrarInformesCommand(Guid.NewGuid(), []);

        var resultado = _validator.Validate(command);

        Assert.False(resultado.IsValid);
    }

    [Fact]
    public void Rechaza_un_archivo_que_excede_el_tamanio_maximo()
    {
        var contenido = new byte[MigrarInformesCommandValidator.TamanioMaximoBytesPorArchivo + 1];
        "%PDF-"u8.CopyTo(contenido);

        var command = new MigrarInformesCommand(Guid.NewGuid(), [new PdfMigrarDto(contenido, "grande.pdf")]);

        var resultado = _validator.Validate(command);

        Assert.False(resultado.IsValid);
    }

    [Fact]
    public void Rechaza_Dependencia_destino_vacia()
    {
        var command = new MigrarInformesCommand(Guid.Empty, [new PdfMigrarDto(ContenidoPdfFalso, "1.pdf")]);

        var resultado = _validator.Validate(command);

        Assert.False(resultado.IsValid);
    }
}
