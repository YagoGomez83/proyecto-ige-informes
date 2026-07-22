using FluentValidation;

namespace IGE.Informes.Application.Informes.Queries.ParsearPdfInforme;

public sealed class ParsearPdfInformeQueryValidator : AbstractValidator<ParsearPdfInformeQuery>
{
    // 20 MB — generoso para un PDF de pocas páginas con imágenes de
    // evidencia embebidas, pero acota el riesgo de un "PDF bomb"
    // (ver docs/06-seguridad-amenazas.md, sección "Ingesta de PDFs").
    public const int TamanioMaximoBytes = 20 * 1024 * 1024;

    private static readonly byte[] FirmaPdf = "%PDF-"u8.ToArray();

    public ParsearPdfInformeQueryValidator()
    {
        RuleFor(x => x.ContenidoPdf)
            .NotEmpty()
            .WithMessage("El archivo no puede estar vacío.");

        RuleFor(x => x.ContenidoPdf)
            .Must(contenido => contenido.Length <= TamanioMaximoBytes)
            .WithMessage($"El archivo supera el tamaño máximo permitido ({TamanioMaximoBytes / 1024 / 1024} MB).")
            .When(x => x.ContenidoPdf.Length > 0);

        RuleFor(x => x.ContenidoPdf)
            .Must(EsPdfReal)
            .WithMessage("El archivo no es un PDF válido (no se reconoce la firma del formato).")
            .When(x => x.ContenidoPdf.Length > 0);
    }

    private static bool EsPdfReal(byte[] contenido) =>
        contenido.Length >= FirmaPdf.Length && contenido.AsSpan(0, FirmaPdf.Length).SequenceEqual(FirmaPdf);
}
