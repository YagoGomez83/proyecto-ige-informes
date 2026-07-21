using FluentValidation;

namespace IGE.Informes.Application.Personas.Commands.IdentificarPersona;

public sealed class IdentificarPersonaCommandValidator : AbstractValidator<IdentificarPersonaCommand>
{
    public IdentificarPersonaCommandValidator()
    {
        RuleFor(x => x.PersonaId).NotEmpty();
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Dni).MaximumLength(20);
    }
}
