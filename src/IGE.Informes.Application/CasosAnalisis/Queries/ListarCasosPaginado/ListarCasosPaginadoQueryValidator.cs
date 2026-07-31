using FluentValidation;

namespace IGE.Informes.Application.CasosAnalisis.Queries.ListarCasosPaginado;

public sealed class ListarCasosPaginadoQueryValidator : AbstractValidator<ListarCasosPaginadoQuery>
{
    public ListarCasosPaginadoQueryValidator()
    {
        RuleFor(x => x.Pagina).GreaterThanOrEqualTo(1);
        RuleFor(x => x.TamanioPagina).InclusiveBetween(1, 100);
    }
}
