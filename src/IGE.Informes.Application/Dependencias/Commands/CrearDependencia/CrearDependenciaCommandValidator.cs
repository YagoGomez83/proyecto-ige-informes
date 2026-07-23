using FluentValidation;

namespace IGE.Informes.Application.Dependencias.Commands.CrearDependencia;

public sealed class CrearDependenciaCommandValidator : AbstractValidator<CrearDependenciaCommand>
{
    public CrearDependenciaCommandValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Tipo).IsInEnum();
    }
}
