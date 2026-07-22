using FluentValidation;

namespace IGE.Informes.Application.Informes.Commands.MigrarInformes;

public sealed class MigrarInformesCommandValidator : AbstractValidator<MigrarInformesCommand>
{
    // Mismo límite que ParsearPdfInformeQueryValidator (HU-01) por archivo
    // individual del lote — mitiga el mismo riesgo de "PDF bomb" descripto
    // en docs/06-seguridad-amenazas.md, sección "Ingesta de PDFs".
    public const int TamanioMaximoBytesPorArchivo = 20 * 1024 * 1024;

    // Límite de cantidad de archivos por corrida — sin esto, la única
    // barrera era la UI (Migrar.razor), lo que permitía un lote de tamaño
    // arbitrario a un cliente que invocara el Command directamente
    // (hallazgo del security-reviewer). Cada PDF se procesa sincrónicamente
    // en el mismo request, así que un lote sin tope es un vector de DoS.
    public const int CantidadMaximaArchivos = 200;

    public MigrarInformesCommandValidator()
    {
        RuleFor(x => x.DependenciaDestinoId).NotEmpty();

        RuleFor(x => x.Pdfs).NotEmpty();

        RuleFor(x => x.Pdfs)
            .Must(pdfs => pdfs.Count <= CantidadMaximaArchivos)
            .WithMessage($"El lote no puede superar los {CantidadMaximaArchivos} archivos por corrida.")
            .When(x => x.Pdfs.Count > 0);

        RuleForEach(x => x.Pdfs).ChildRules(pdf =>
        {
            pdf.RuleFor(p => p.NombreArchivo).NotEmpty().MaximumLength(255);
            pdf.RuleFor(p => p.Contenido).NotEmpty();
            pdf.RuleFor(p => p.Contenido)
                .Must(contenido => contenido.Length <= TamanioMaximoBytesPorArchivo)
                .WithMessage($"El archivo supera el tamaño máximo permitido ({TamanioMaximoBytesPorArchivo / 1024 / 1024} MB).")
                .When(p => p.Contenido.Length > 0);
        });
    }
}
