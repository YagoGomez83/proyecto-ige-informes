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
    //
    // El Command completo (todos los PdfMigrarDto.Contenido) se materializa
    // en memoria de una sola vez ANTES de llegar al Handler — no hay
    // streaming, y los 100+ byte[] crudos permanecen referenciados por
    // request.Pdfs durante todo el foreach (no se liberan archivo por
    // archivo). Con el servidor de producción real en 6 GB de RAM total
    // (Postgres + MinIO + la app en el mismo host, ver ADR-003):
    //   - El tope se calcula sobre los bytes CRUDOS del lote, no sobre el
    //     pico real de memoria administrada: PdfPig típicamente decodifica
    //     cada PDF a un múltiplo (3-5x) de su tamaño en disco (objetos,
    //     streams descomprimidos, glifos), y Postgres/MinIO reservan sus
    //     propios buffers bajo carga concurrente real (no solo en reposo).
    //   - Presupuesto de bytes crudos: ~800 MB (que con el factor de PdfPig
    //     puede traducirse a ~3 GB reales en el peor caso), dejando el resto
    //     de los 6 GB para SO + Postgres + MinIO + GC de .NET bajo carga.
    // A TamanioMaximoBytesPorArchivo (20 MB), eso da un tope de 40 archivos
    // por corrida (bajado desde 500, y luego desde un primer ajuste a 100
    // que no contemplaba el overhead de PdfPig — hallazgo del
    // security-reviewer). Sin medición empírica contra el stack real bajo
    // carga: si se mide y sobra margen, se puede subir de nuevo. Si hace
    // falta migrar más por vez, dividir en varias corridas hasta que se
    // rediseñe el flujo para procesar de a un archivo (deuda pendiente, ver
    // docs/08-plan-implementacion.md).
    public const int CantidadMaximaArchivos = 40;

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
