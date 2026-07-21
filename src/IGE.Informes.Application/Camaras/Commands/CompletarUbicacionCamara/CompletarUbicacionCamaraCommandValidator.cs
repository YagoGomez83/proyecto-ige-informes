using FluentValidation;

namespace IGE.Informes.Application.Camaras.Commands.CompletarUbicacionCamara;

public sealed class CompletarUbicacionCamaraCommandValidator : AbstractValidator<CompletarUbicacionCamaraCommand>
{
    public CompletarUbicacionCamaraCommandValidator()
    {
        RuleFor(x => x.CamaraId).NotEmpty();
        RuleFor(x => x.Ubicacion).NotEmpty().MaximumLength(300);
    }
}
