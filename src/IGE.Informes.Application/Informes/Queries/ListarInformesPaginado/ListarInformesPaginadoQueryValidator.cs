using FluentValidation;

namespace IGE.Informes.Application.Informes.Queries.ListarInformesPaginado;

public sealed class ListarInformesPaginadoQueryValidator : AbstractValidator<ListarInformesPaginadoQuery>
{
    public ListarInformesPaginadoQueryValidator()
    {
        RuleFor(x => x.Pagina).GreaterThanOrEqualTo(1);
        RuleFor(x => x.TamanioPagina).InclusiveBetween(1, 100);
    }
}
