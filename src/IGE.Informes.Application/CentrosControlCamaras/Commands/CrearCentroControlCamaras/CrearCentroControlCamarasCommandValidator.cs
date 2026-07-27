using FluentValidation;

namespace IGE.Informes.Application.CentrosControlCamaras.Commands.CrearCentroControlCamaras;

public sealed class CrearCentroControlCamarasCommandValidator : AbstractValidator<CrearCentroControlCamarasCommand>
{
    public CrearCentroControlCamarasCommandValidator()
    {
        RuleFor(x => x.Sigla).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(200);
    }
}
