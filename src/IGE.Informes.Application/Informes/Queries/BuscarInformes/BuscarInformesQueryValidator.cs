using FluentValidation;

namespace IGE.Informes.Application.Informes.Queries.BuscarInformes;

public sealed class BuscarInformesQueryValidator : AbstractValidator<BuscarInformesQuery>
{
    public BuscarInformesQueryValidator()
    {
        RuleFor(x => x)
            .Must(x => x.FechaDesde is null || x.FechaHasta is null || x.FechaDesde <= x.FechaHasta)
            .WithMessage("La fecha 'desde' no puede ser posterior a la fecha 'hasta'.");

        RuleFor(x => x.DniOPersona).MaximumLength(200);
        RuleFor(x => x.DominioVehiculo).MaximumLength(20);
        RuleFor(x => x.TextoLibre).MaximumLength(500);
    }
}
