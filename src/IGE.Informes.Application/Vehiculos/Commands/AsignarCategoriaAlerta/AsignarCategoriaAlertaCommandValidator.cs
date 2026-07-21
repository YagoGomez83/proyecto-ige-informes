using FluentValidation;

namespace IGE.Informes.Application.Vehiculos.Commands.AsignarCategoriaAlerta;

public sealed class AsignarCategoriaAlertaCommandValidator : AbstractValidator<AsignarCategoriaAlertaCommand>
{
    public AsignarCategoriaAlertaCommandValidator()
    {
        RuleFor(x => x.VehiculoId).NotEmpty();
        RuleFor(x => x.CategoriaAlertaId).NotEmpty();
    }
}
