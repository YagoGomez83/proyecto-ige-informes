using FluentValidation;

namespace IGE.Informes.Application.Personas.Queries.ListarPersonas;

public sealed class ListarPersonasQueryValidator : AbstractValidator<ListarPersonasQuery>
{
    public ListarPersonasQueryValidator()
    {
        RuleFor(x => x.Pagina).GreaterThanOrEqualTo(1);
        RuleFor(x => x.TamanioPagina).InclusiveBetween(1, 100);
    }
}
