using FluentValidation;

namespace IGE.Informes.Application.Informes.Commands.VincularPersonaInforme;

public sealed class VincularPersonaInformeCommandValidator : AbstractValidator<VincularPersonaInformeCommand>
{
    public VincularPersonaInformeCommandValidator()
    {
        RuleFor(x => x.InformeId).NotEmpty();
        RuleFor(x => x.PersonaId).NotEmpty();
    }
}
