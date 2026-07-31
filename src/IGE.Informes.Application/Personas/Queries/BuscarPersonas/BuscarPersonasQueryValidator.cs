using FluentValidation;

namespace IGE.Informes.Application.Personas.Queries.BuscarPersonas;

public sealed class BuscarPersonasQueryValidator : AbstractValidator<BuscarPersonasQuery>
{
    public BuscarPersonasQueryValidator()
    {
        // Mínimo de 3 caracteres: evita que un término trivial (1-2
        // caracteres) devuelva DNIs de personas sin relación real con lo
        // buscado (hallazgo del security-reviewer — este DTO expone Dni).
        RuleFor(x => x.TextoLibre).NotEmpty().MinimumLength(3).MaximumLength(500);
    }
}
