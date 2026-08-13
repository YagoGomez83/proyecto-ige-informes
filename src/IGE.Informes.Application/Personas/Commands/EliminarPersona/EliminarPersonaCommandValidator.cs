using FluentValidation;

namespace IGE.Informes.Application.Personas.Commands.EliminarPersona;

public sealed class EliminarPersonaCommandValidator : AbstractValidator<EliminarPersonaCommand>
{
    public EliminarPersonaCommandValidator()
    {
        RuleFor(x => x.PersonaId).NotEmpty();
    }
}
