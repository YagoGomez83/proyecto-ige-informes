using FluentValidation;

namespace IGE.Informes.Application.Camaras.Commands.CambiarTipoCamara;

public sealed class CambiarTipoCamaraCommandValidator : AbstractValidator<CambiarTipoCamaraCommand>
{
    public CambiarTipoCamaraCommandValidator()
    {
        RuleFor(x => x.CamaraId).NotEmpty();
        RuleFor(x => x.NuevoTipo).IsInEnum();
    }
}
