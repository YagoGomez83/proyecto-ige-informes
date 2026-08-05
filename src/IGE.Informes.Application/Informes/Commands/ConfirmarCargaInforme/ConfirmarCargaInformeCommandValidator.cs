using FluentValidation;

namespace IGE.Informes.Application.Informes.Commands.ConfirmarCargaInforme;

public sealed class ConfirmarCargaInformeCommandValidator : AbstractValidator<ConfirmarCargaInformeCommand>
{
    public ConfirmarCargaInformeCommandValidator()
    {
        RuleFor(x => x.ContenidoPdf).NotEmpty();
        RuleFor(x => x.NombreArchivo).NotEmpty().MaximumLength(255);
        RuleFor(x => x.IdRegistro).NotEmpty().Matches(@"^\d+/\d{4}$")
            .WithMessage("El ID Registro debe tener el formato NNN/AAAA.");
        // Nullable a propósito: un PDF histórico completado a mano no tiene
        // Caso de Análisis de origen, igual que los migrados en lote (HU-04)
        // — ver comentario en ConfirmarCargaInformeCommand. Cuando viene
        // informado, no puede ser Guid.Empty.
        RuleFor(x => x.CasoAnalisisId).NotEqual(Guid.Empty).When(x => x.CasoAnalisisId is not null);
        RuleFor(x => x.DependenciaDestinoId).NotEmpty();
        RuleFor(x => x.Relato).MaximumLength(8000);
        RuleFor(x => x.CausaCaratula).MaximumLength(500);
        RuleFor(x => x.CausaNroPiezaSumarial).MaximumLength(100);
        RuleFor(x => x.CausaCircunscripcionJudicial).MaximumLength(200);

        RuleForEach(x => x.Evidencias).ChildRules(evidencia =>
        {
            evidencia.RuleFor(e => e.NumeroImagen).GreaterThan(0);
            evidencia.RuleFor(e => e.Descripcion).MaximumLength(2000);
        });
    }
}
