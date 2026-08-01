using FluentValidation;

namespace IGE.Informes.Application.Informes.Commands.VincularVehiculoInforme;

public sealed class VincularVehiculoInformeCommandValidator : AbstractValidator<VincularVehiculoInformeCommand>
{
    public VincularVehiculoInformeCommandValidator()
    {
        RuleFor(x => x.InformeId).NotEmpty();
        RuleFor(x => x.VehiculoId).NotEmpty();
    }
}
