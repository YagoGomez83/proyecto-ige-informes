using FluentValidation;

namespace IGE.Informes.Application.Localidades.Commands.CrearLocalidad;

public sealed class CrearLocalidadCommandValidator : AbstractValidator<CrearLocalidadCommand>
{
    public CrearLocalidadCommandValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(200);
    }
}
