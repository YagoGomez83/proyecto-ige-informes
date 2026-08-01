using FluentValidation;

namespace IGE.Informes.Application.Vehiculos.Commands.QuitarImagenVehiculo;

public sealed class QuitarImagenVehiculoCommandValidator : AbstractValidator<QuitarImagenVehiculoCommand>
{
    public QuitarImagenVehiculoCommandValidator()
    {
        RuleFor(x => x.VehiculoImagenId).NotEmpty();
    }
}
