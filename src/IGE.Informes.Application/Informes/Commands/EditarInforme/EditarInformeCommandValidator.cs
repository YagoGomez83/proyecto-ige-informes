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
    }
}
