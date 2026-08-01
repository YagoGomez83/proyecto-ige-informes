using FluentValidation;

namespace IGE.Informes.Application.Alertas.Queries.ListarAlertas;

public sealed class ListarAlertasQueryValidator : AbstractValidator<ListarAlertasQuery>
{
    public ListarAlertasQueryValidator()
    {
        RuleFor(x => x.Pagina).GreaterThanOrEqualTo(1);
        RuleFor(x => x.TamanioPagina).InclusiveBetween(1, 100);
    }
}
