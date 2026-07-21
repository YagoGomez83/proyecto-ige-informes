using FluentValidation;

namespace IGE.Informes.Application.CasosAnalisis.Commands.VincularVehiculoTexto;

public sealed class VincularVehiculoTextoCommandValidator : AbstractValidator<VincularVehiculoTextoCommand>
{
    public VincularVehiculoTextoCommandValidator()
    {
        RuleFor(x => x.CasoId).NotEmpty();
        RuleFor(x => x.DescripcionLibre).NotEmpty().MaximumLength(1000);
    }
}
