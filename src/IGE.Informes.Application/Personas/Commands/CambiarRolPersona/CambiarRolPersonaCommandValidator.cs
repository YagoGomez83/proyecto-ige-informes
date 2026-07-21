using FluentValidation;

namespace IGE.Informes.Application.Personas.Commands.CambiarRolPersona;

public sealed class CambiarRolPersonaCommandValidator : AbstractValidator<CambiarRolPersonaCommand>
{
    public CambiarRolPersonaCommandValidator()
    {
        RuleFor(x => x.PersonaId).NotEmpty();
        RuleFor(x => x.NuevoRol).IsInEnum();
    }
}
