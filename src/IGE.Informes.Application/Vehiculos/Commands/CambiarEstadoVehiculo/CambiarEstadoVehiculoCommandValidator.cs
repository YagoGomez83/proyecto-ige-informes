using FluentValidation;

namespace IGE.Informes.Application.Vehiculos.Commands.CambiarEstadoVehiculo;

public sealed class CambiarEstadoVehiculoCommandValidator : AbstractValidator<CambiarEstadoVehiculoCommand>
{
    public CambiarEstadoVehiculoCommandValidator()
    {
        RuleFor(x => x.VehiculoId).NotEmpty();
        RuleFor(x => x.NuevoEstado).IsInEnum();
    }
}
