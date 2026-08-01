using FluentValidation;

namespace IGE.Informes.Application.Personas.Commands.VincularVehiculo;

public sealed class VincularVehiculoACommandValidator : AbstractValidator<VincularVehiculoACommand>
{
    public VincularVehiculoACommandValidator()
    {
        RuleFor(x => x.PersonaId).NotEmpty();
        RuleFor(x => x.VehiculoId).NotEmpty();
    }
}
