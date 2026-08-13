using FluentValidation;

namespace IGE.Informes.Application.Vehiculos.Commands.EliminarVehiculo;

public sealed class EliminarVehiculoCommandValidator : AbstractValidator<EliminarVehiculoCommand>
{
    public EliminarVehiculoCommandValidator()
    {
        RuleFor(x => x.VehiculoId).NotEmpty();
    }
}
