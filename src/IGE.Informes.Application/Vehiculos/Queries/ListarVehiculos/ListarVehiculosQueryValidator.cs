using FluentValidation;

namespace IGE.Informes.Application.Vehiculos.Queries.ListarVehiculos;

public sealed class ListarVehiculosQueryValidator : AbstractValidator<ListarVehiculosQuery>
{
    public ListarVehiculosQueryValidator()
    {
        RuleFor(x => x.Pagina).GreaterThanOrEqualTo(1);
        RuleFor(x => x.TamanioPagina).InclusiveBetween(1, 100);
    }
}
