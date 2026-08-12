using FluentValidation;

namespace IGE.Informes.Application.Dependencias.Commands.EliminarDependencia;

public sealed class EliminarDependenciaCommandValidator : AbstractValidator<EliminarDependenciaCommand>
{
    public EliminarDependenciaCommandValidator()
    {
        RuleFor(x => x.DependenciaId).NotEmpty();
    }
}
