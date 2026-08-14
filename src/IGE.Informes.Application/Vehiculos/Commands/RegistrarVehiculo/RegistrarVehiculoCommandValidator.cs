using FluentValidation;
using IGE.Informes.Domain.Entities;

namespace IGE.Informes.Application.Vehiculos.Commands.RegistrarVehiculo;

public sealed class RegistrarVehiculoCommandValidator : AbstractValidator<RegistrarVehiculoCommand>
{
    public RegistrarVehiculoCommandValidator()
    {
        RuleFor(x => x.Marca).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Modelo).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Color).NotEmpty().MaximumLength(50);
        RuleFor(x => x.AvisarA).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Dominio).MaximumLength(20);
        RuleFor(x => x.Caracteristicas).MaximumLength(2000);
        RuleFor(x => x.DominioCerteza).IsInEnum();
        RuleFor(x => x.AccionARealizar).IsInEnum();
        RuleFor(x => x.TipoVehiculo).IsInEnum();
        RuleFor(x => x.Cilindrada).MaximumLength(20);
        RuleFor(x => x.Cilindrada)
            .NotEmpty()
            .WithMessage("La cilindrada es obligatoria para motocicletas.")
            .When(x => x.TipoVehiculo == TipoVehiculo.Moto);
    }
}
