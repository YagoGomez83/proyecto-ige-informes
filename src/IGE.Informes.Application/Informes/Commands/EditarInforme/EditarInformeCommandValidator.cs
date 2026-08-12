using FluentValidation;

namespace IGE.Informes.Application.Informes.Commands.EditarInforme;

public sealed class EditarInformeCommandValidator : AbstractValidator<EditarInformeCommand>
{
    public EditarInformeCommandValidator()
    {
        RuleFor(x => x.InformeId).NotEmpty();
        RuleFor(x => x.Relato).MaximumLength(8000);
        RuleFor(x => x.CausaCaratula).MaximumLength(500);
        RuleFor(x => x.CausaNroPiezaSumarial).MaximumLength(100);
        RuleFor(x => x.CausaCircunscripcionJudicial).MaximumLength(200);
        RuleFor(x => x.FechaAnalisis)
            .Must(fecha => fecha <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("La Fecha de Análisis no puede ser futura.")
            .When(x => x.FechaAnalisis is not null);

        // La Carátula puede completarse sin N° de Pieza Sumarial (hay
        // Dependencias/tipos de análisis, ej. Narcotráfico, que no aportan
        // un número de expediente real — ver docs/03-modelo-dominio.md,
        // "Causa.NroPiezaSumarial pasa a ser opcional"). El sentido
        // inverso sigue sin tener sentido: un N° de Pieza Sumarial sin
        // saber de qué Carátula se trata.
        RuleFor(x => x.CausaCaratula)
            .NotEmpty()
            .WithMessage("La Carátula es obligatoria para guardar el N° de Pieza Sumarial de la Causa.")
            .When(x => !string.IsNullOrWhiteSpace(x.CausaNroPiezaSumarial));
    }
}
