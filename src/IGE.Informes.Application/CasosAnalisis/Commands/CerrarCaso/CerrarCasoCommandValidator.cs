using FluentValidation;

namespace IGE.Informes.Application.CasosAnalisis.Commands.CerrarCaso;

public sealed class CerrarCasoCommandValidator : AbstractValidator<CerrarCasoCommand>
{
    public CerrarCasoCommandValidator()
    {
        RuleFor(x => x.CasoId).NotEmpty();
        RuleFor(x => x.Resultado).IsInEnum();
    }
}
