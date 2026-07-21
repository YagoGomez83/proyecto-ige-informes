using FluentValidation;

namespace IGE.Informes.Application.Personas.Commands.RegistrarPersona;

public sealed class RegistrarPersonaCommandValidator : AbstractValidator<RegistrarPersonaCommand>
{
    public RegistrarPersonaCommandValidator()
    {
        RuleFor(x => x.Rol).IsInEnum();
        RuleFor(x => x.Nombre).MaximumLength(200);
        RuleFor(x => x.Dni).MaximumLength(20);
        RuleFor(x => x.Caracteristicas).MaximumLength(2000);

        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Nombre) || !string.IsNullOrWhiteSpace(x.Caracteristicas))
            .WithMessage("Una Persona sin identificar debe tener al menos una característica descriptiva.");
    }
}
