using FluentValidation;

namespace IGE.Informes.Application.Camaras.Commands.RegistrarCamara;

public sealed class RegistrarCamaraCommandValidator : AbstractValidator<RegistrarCamaraCommand>
{
    public RegistrarCamaraCommandValidator()
    {
        RuleFor(x => x.Codigo).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Tipo).IsInEnum();
        RuleFor(x => x.Ubicacion).MaximumLength(300);
    }
}
