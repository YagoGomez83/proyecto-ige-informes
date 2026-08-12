using FluentValidation;

namespace IGE.Informes.Application.MarcaVehiculo.Commands.CrearMarcaVehiculo;

public sealed class CrearMarcaVehiculoCommandValidator : AbstractValidator<CrearMarcaVehiculoCommand>
{
    public CrearMarcaVehiculoCommandValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(200);
    }
}
