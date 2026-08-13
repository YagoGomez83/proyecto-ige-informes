using FluentValidation;

namespace IGE.Informes.Application.Usuarios.Commands.CambiarPasswordPropia;

public sealed class CambiarPasswordPropiaCommandValidator : AbstractValidator<CambiarPasswordPropiaCommand>
{
    public CambiarPasswordPropiaCommandValidator()
    {
        RuleFor(x => x.PasswordActual).NotEmpty();
        RuleFor(x => x.PasswordNueva).NotEmpty().MinimumLength(12);
    }
}
