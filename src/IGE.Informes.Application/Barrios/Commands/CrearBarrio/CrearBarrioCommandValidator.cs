using FluentValidation;

namespace IGE.Informes.Application.Barrios.Commands.CrearBarrio;

public sealed class CrearBarrioCommandValidator : AbstractValidator<CrearBarrioCommand>
{
    public CrearBarrioCommandValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(200);
    }
}
