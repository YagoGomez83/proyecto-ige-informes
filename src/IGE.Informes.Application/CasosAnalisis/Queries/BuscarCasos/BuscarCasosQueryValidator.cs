using FluentValidation;

namespace IGE.Informes.Application.CasosAnalisis.Queries.BuscarCasos;

public sealed class BuscarCasosQueryValidator : AbstractValidator<BuscarCasosQuery>
{
    public BuscarCasosQueryValidator()
    {
        // Mínimo de 3 caracteres: evita matches triviales, mismo criterio
        // aplicado a las 3 Queries de búsqueda nuevas (hallazgo del
        // security-reviewer).
        RuleFor(x => x.TextoLibre).NotEmpty().MinimumLength(3).MaximumLength(500);
    }
}
