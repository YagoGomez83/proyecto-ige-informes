using FluentValidation;

namespace IGE.Informes.Application.Camaras.Queries.ListarCamarasPaginado;

public sealed class ListarCamarasPaginadoQueryValidator : AbstractValidator<ListarCamarasPaginadoQuery>
{
    public ListarCamarasPaginadoQueryValidator()
    {
        RuleFor(x => x.Pagina).GreaterThanOrEqualTo(1);
        RuleFor(x => x.TamanioPagina).InclusiveBetween(1, 100);
    }
}
