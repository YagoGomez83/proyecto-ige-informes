using FluentValidation;

namespace IGE.Informes.Application.Vehiculos.Commands.DarDeBajaVehiculo;

public sealed class DarDeBajaVehiculoCommandValidator : AbstractValidator<DarDeBajaVehiculoCommand>
{
    public DarDeBajaVehiculoCommandValidator()
    {
        RuleFor(x => x.VehiculoId).NotEmpty();
        RuleFor(x => x.FechaBaja).NotEqual(default(DateOnly));
    }
}
