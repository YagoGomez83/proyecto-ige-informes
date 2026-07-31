using FluentValidation;

namespace IGE.Informes.Application.Vehiculos.Queries.BuscarVehiculos;

public sealed class BuscarVehiculosQueryValidator : AbstractValidator<BuscarVehiculosQuery>
{
    public BuscarVehiculosQueryValidator()
    {
        // Mínimo de 3 caracteres: evita matches triviales que devuelvan
        // resultados sin relación real con lo buscado (hallazgo del
        // security-reviewer, más relevante aún en BuscarPersonasQuery que
        // expone el Dni en el resultado).
        RuleFor(x => x.TextoLibre).NotEmpty().MinimumLength(3).MaximumLength(500);
    }
}
