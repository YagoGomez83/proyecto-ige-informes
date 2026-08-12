using FluentValidation;

namespace IGE.Informes.Application.Informes.Commands.GenerarInformeDesdeCaso;

public sealed class GenerarInformeDesdeCasoCommandValidator : AbstractValidator<GenerarInformeDesdeCasoCommand>
{
    public GenerarInformeDesdeCasoCommandValidator()
    {
        RuleFor(x => x.CasoAnalisisId).NotEmpty();
        RuleFor(x => x.DependenciaDestinoId).NotEmpty();
        RuleFor(x => x.CausaCaratula).NotEmpty().MaximumLength(500);
        RuleFor(x => x.CausaNroPiezaSumarial).MaximumLength(100);
        RuleFor(x => x.CausaCircunscripcionJudicial).MaximumLength(200);
    }
}
