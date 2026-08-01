using FluentValidation;

namespace IGE.Informes.Application.Personas.Commands.QuitarImagenPersona;

public sealed class QuitarImagenPersonaCommandValidator : AbstractValidator<QuitarImagenPersonaCommand>
{
    public QuitarImagenPersonaCommandValidator()
    {
        RuleFor(x => x.PersonaImagenId).NotEmpty();
    }
}
