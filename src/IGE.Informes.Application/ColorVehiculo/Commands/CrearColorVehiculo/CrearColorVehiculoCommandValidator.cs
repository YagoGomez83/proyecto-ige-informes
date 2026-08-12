using FluentValidation;

namespace IGE.Informes.Application.ColorVehiculo.Commands.CrearColorVehiculo;

public sealed class CrearColorVehiculoCommandValidator : AbstractValidator<CrearColorVehiculoCommand>
{
    public CrearColorVehiculoCommandValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(200);
    }
}
