using FluentValidation;

namespace IGE.Informes.Application.Dependencias.Queries.ListarDependenciasPaginado;

public sealed class ListarDependenciasPaginadoQueryValidator : AbstractValidator<ListarDependenciasPaginadoQuery>
{
    public ListarDependenciasPaginadoQueryValidator()
    {
        RuleFor(x => x.Pagina).GreaterThanOrEqualTo(1);
        RuleFor(x => x.TamanioPagina).InclusiveBetween(1, 100);
    }
}
